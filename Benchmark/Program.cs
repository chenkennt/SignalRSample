namespace Client
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.SignalR.Client;
    using Microsoft.AspNetCore.Sockets.Client;
    using Newtonsoft.Json;

    class Program
    {
        class Counter
        {
            DateTime window = DateTime.MinValue;

            DateTime begin = DateTime.MinValue;

            long allSum = 0;

            int allCount = 0;

            List<long> data = new List<long>();

            int unit;

            string name;

            DateTime Floor(DateTime dt)
            {
                var sec = dt.Minute * 60 + dt.Second;
                var floored = sec / unit * unit;
                return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, floored / 60, floored % 60);
            }

            public Counter(string name, int unit)
            {
                this.name = name;
                this.unit = unit;
            }

            public void Add(long value)
            {
                List<long> last = null;
                lock (this)
                {
                    var now = Floor(DateTime.Now);
                    if (begin == DateTime.MinValue) begin = now;
                    if (window != DateTime.MinValue && window != now)
                    {
                        last = data;
                        data = new List<long>();
                    }

                    window = now;
                    data.Add(value);
                }

                if (last != null)
                {
                    last.Sort();
                    var count = last.Count();
                    var sum = last.Sum();
                    allCount += count;
                    allSum += sum;
                    long avg = sum / count, d95 = last[(int)(count * 0.95)], d99 = last[(int)(count * 0.99)];
                    Console.WriteLine($"[{window}] {name}: 99%: {d99}ms, 95%: {d95}ms, avg: {sum / count}ms, count: {count}, processing: {processing}");
                }
            }
        }

        static int processing;

        static Counter received;

        static HubConnection[] connection;

        [ThreadStatic]
        static Random rand;

        static Random Rand
        {
            get
            {
                if (rand == null) rand = new Random((int)DateTime.Now.Ticks);
                return rand;
            }
        }

        static Stopwatch sw = new Stopwatch();

        static string endpoint;

        static int count;

        static int sendInterval;

        static int statInterval;

        static string protocol;

        static string serviceEndpoint;

        static string bearer;

        static async Task Connect()
        {
            HttpClient c = new HttpClient();
            var conn = JsonConvert.DeserializeObject<Dictionary<string, string>>(await c.GetStringAsync(endpoint));
            serviceEndpoint = conn["serviceUrl"];
            bearer = conn["jwtBearer"];

            Stopwatch w = new Stopwatch();
            w.Start();
            connection = new HubConnection[count];
            for (var i = 0; i < count; i++)
            {
                var builder = new HubConnectionBuilder().WithUrl(serviceEndpoint).WithJwtBearer(() => bearer);
                builder = protocol.ToLower() == "json" ? builder.WithJsonProtocol() : builder.WithMessagePackProtocol();
                connection[i] = builder.Build();
                try
                {
                    await connection[i].StartAsync();
                    connection[i].On<string, string>("echo", (started, message) => {
                        Interlocked.Decrement(ref processing);
                        received.Add(sw.ElapsedMilliseconds - int.Parse(started));
                    });
                }
                catch (Exception ex)
                {
                    if (ex is HttpRequestException && ex.Message.Contains("401 (Unauthorized)")) bearer = JsonConvert.DeserializeObject<Dictionary<string, string>>(await c.GetStringAsync(endpoint))["jwtBearer"];
                    i--;
                    Console.WriteLine("Connect failed with the following message:");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("Retry in one second.");
                    await Task.Delay(1000);
                }
                if ((i + 1) % 100 == 0)
                {
                    Console.WriteLine($"{i + 1} connected, time elapsed: {w.Elapsed}");
                }
            }
        }

        static async Task Send(HubConnection c)
        {
            while (true)
            {
                Interlocked.Increment(ref processing);
                c.InvokeAsync("echo", sw.ElapsedMilliseconds.ToString(), "Sample Payload");
                await Task.Delay(Rand.Next(sendInterval * 2));
            }
        }

        static async Task Run()
        {
            await Connect();
            sw.Start();
            var ts = new Task[count];
            for (var i = 0; i < count; i++) ts[i] = Send(connection[i]);
            Console.ReadKey();
        }

        static void Main(string[] args)
        {
            if (args.Length < 5)
            {
                Console.WriteLine("Usage: client [server_endpoint] [connection_count] [send_interval(ms)] [stat_interval(sec)] [protocol]");
                return;
            }

            Console.WriteLine("Start at " + DateTime.Now + ", press any key to stop...");
            endpoint = args[0];
            count = int.Parse(args[1]);
            sendInterval = int.Parse(args[2]);
            statInterval = int.Parse(args[3]);
            protocol = args[4];
            received = new Counter("recv", statInterval);
            Run().Wait();
        }
    }
}
