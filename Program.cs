using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Lab15
{
    
    class Program
    {
        public static ManualResetEventSlim res1 = new ManualResetEventSlim(false);
        public static ManualResetEventSlim res2 = new ManualResetEventSlim(true);
        //public static Barrier barrier = new Barrier(2);
        public static SemaphoreSlim sem = new SemaphoreSlim(1);
        
        static List<uint> SieveEratosthenes(uint n) // Решето Эратосфена прямиком из тырнетиков
        {
            var numbers = new List<uint>();
            //заполнение списка числами от 2 до n-1
            for (var i = 2u; i < n; i++)
            {
                numbers.Add(i);
            }

            for (var i = 0; i < numbers.Count; i++)
            {
                for (var j = 2u; j < n; j++)
                {
                    //удаляем кратные числа из списка
                    numbers.Remove(numbers[i] * j);
                }
            }

            return numbers;
        }
        public static void ThreadPrimeNumbers()
        {
            var curThread = Thread.CurrentThread;
            Console.WriteLine($"Thread {curThread.Name} is running\n{curThread.ManagedThreadId} | {curThread.Priority} | {curThread.ThreadState}");
            
            //Console.ReadKey(); // Дежавю по плюсам, getchar() чтобы эта машина не жрала мой поток
            Console.Write("Write n: ");
            uint n;
            string s;
            
            s = Console.ReadLine();
            try
            {
               n = uint.Parse(s);
               //Console.WriteLine(string.Join(" ", SieveEratosthenes(n)));
               foreach (uint number in SieveEratosthenes(n))
                {
                    Console.Write(number + " ");
                    Thread.Sleep(30);
                }
                Console.WriteLine();
            } catch (Exception e)
            {
                Console.WriteLine(e);
                Thread.CurrentThread.Abort();
            }

            
        
        }

        public static void ThreadEven(uint n, bool Synced = false)
        {
            if (Synced)
            {
                for (int i = 2; i < n; i += 2)
                {
                    res1.Wait();
                    sem.Wait();
                    Console.Write(i + " ");
                    using (var file = new StreamWriter("synced.txt", true))
                    {
                        
                        file.Write(i + " ");
                    }
                    sem.Release();
                    res2.Set();
                    res1.Reset();
                   
                   
                }
             
            }
            else
            { 
                for (int i = 2; i < n; i+=2)
                {
                    Console.Write(i + " ");

                    //Thread.Sleep(7); 
                }
            }
        }
        public static void ThreadUneven(uint n, bool Synced = false)
        {
            if (Synced)
            {
                for (int i = 1; i < n; i += 2)
                {
                    res2.Wait();
                    sem.Wait();
                    Console.Write(i + " ");
                    using (var file = new StreamWriter("synced.txt", true))
                    {
                        file.Write(i + " ");
                    }
                    sem.Release();
                    res1.Set();
                    res2.Reset();
                    
                }
                
            }
            else
            {
                for (int i = 1; i < n; i += 2)
                {
                    Console.Write(i + " ");
                    //Thread.Sleep(15);
                }
            }
        }

        static void Main(string[] args)
        {
            File.WriteAllText("synced.txt", "");
            var processes = Process.GetProcesses();

            foreach (Process process in processes)
            {
                try
                {
                    Console.WriteLine($"{process.Id} | {process.ProcessName} | {process.BasePriority} | {process.Responding} | {process.StartTime} | {process.TotalProcessorTime}");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            var curDomain = System.AppDomain.CurrentDomain;
            Console.WriteLine($"{curDomain.FriendlyName} | {curDomain.SetupInformation}\n\t{string.Join("\n\t", curDomain.GetAssemblies().AsEnumerable())}");

            try
            {
                var newDomain = System.AppDomain.CreateDomain("NewDomain");
                newDomain.Load(System.Reflection.Assembly.GetExecutingAssembly().GetName());
                AppDomain.Unload(newDomain);
                
            } catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            var th1 = new Thread(new ThreadStart(ThreadPrimeNumbers));
            th1.Start();
            try
            { 
            th1.Suspend();
            Thread.Sleep(1000);
            th1.Resume();
            } catch (Exception e)
            {
                while (th1.IsAlive)
                {
                    Thread.Sleep(2000);
                }
                Console.WriteLine(e.Message);
            }
           

            th1 = new Thread(() => ThreadEven(10));
            var th2 = new Thread(() => ThreadUneven(10));
            th1.Priority = ThreadPriority.AboveNormal;
            
            th1.Start();
            th2.Start();
            th1.Join();
            th2.Join();

            Console.WriteLine();
            th1 = new Thread(() => ThreadEven(10, true));
            th2 = new Thread(() => ThreadUneven(10, true));
            th1.Start();
            th2.Start();
            th1.Join();
            th2.Join();



            Console.WriteLine();
            var timer = new Timer(new TimerCallback(ShowTime), null, 0, 300);
            Thread.Sleep(1500);
            timer.Dispose();

            


            using (var file = new StreamWriter("warehouse.txt"))
            {
                for (int i = 0; i < 30; i++)
                {
                    file.Write($"{i}\n");
                }
            }
            var rand = new Random();
            var rand1 = rand.Next(3, 12);
            var rand2 = rand.Next(3, 12);
            var rand3 = 30 - rand1 - rand2;
            th1 = new Thread(() => ThreadCar(rand1, 1));
            th2 = new Thread(() => ThreadCar(rand2, 2));
            var th3 = new Thread(() => ThreadCar(rand3, 3));

            th1.Start();
            th2.Start();
            th3.Start();

            th1.Join();
            th2.Join();
            th3.Join();

            Videos videos = new Videos(3);

            for (int i = 0; i < 10; i++)
            {
                new Thread(() => Viewer(i, videos)).Start();
                Thread.Sleep(10);
            }

            Console.WriteLine("Press any key to finish...");
            
            Console.ReadKey();
        }
        // Расскидал тут классов c методами в разные стороны, хоть бы прибрался ё-маё

        public static void ThreadCar(int capacity, int id)
        {
            sem.Wait();

            var warehouse = File.ReadAllLines("warehouse.txt").ToList();
            var rand = new Random();
            
            for (int i = 0; i < capacity; i++)
            {
                var crate = rand.Next(0, warehouse.Count);
                Console.WriteLine($"Car {id} picks up crate {warehouse[crate]}");
                warehouse.RemoveAt(crate);
            }
            Console.WriteLine($"Car {id} is fully loaded, departing...");
            using (var file = new StreamWriter("warehouse.txt"))
            {
                foreach (string crate in warehouse)
                {
                    file.Write($"{crate}\n");
                }
            }
            sem.Release();
        }

        public static SemaphoreSlim sem2 = new SemaphoreSlim(3);
       
    

        public static void Viewer(int id, Videos videos)
        {
            id++;
            Console.WriteLine($"Viewer {id} has been born");
            if (sem2.Wait(3000))
            {
                int video = videos.Watch;
                Console.WriteLine($"Viewer {id} is currently watching video {video+1}");
                Thread.Sleep(2000);
                Console.WriteLine($"Viewer {id} has finished watching video {video+1}");
                videos.Finish(video);
                sem2.Release();
            }
           else
            {
                Console.WriteLine($"Viewer {id} left unhappy"); //happyn't
            }
            
            
        }

        public class Videos
        {
            public List<SemaphoreSlim> semList = new List<SemaphoreSlim>();
            public SemaphoreSlim this [int i]
            {
                get => semList[i];
            }
            public Videos(int n)
            {
                for (int i = 0; i < n; i++)
                    semList.Add(new SemaphoreSlim(1));
            }

            public int Watch
            {
                get 
                {
                    for (int i = 0; i < semList.Count; i++)
                    {
                        if (semList[i].CurrentCount == 1) // if semaphore is free
                        {
                            semList[i].Wait();
                            return i;
                        }

                    }
                    return -1;
                }
            }

            public void Finish(int i)
            {
                semList[i].Release();
            }
        }
        public static void ShowTime(object state)
        {
            Console.WriteLine(System.DateTime.Now.ToString("G"));
        }


        
    }
}
