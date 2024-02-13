using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Windows;
using ConwayGOL.App_Start;
using MoreLinq;

namespace Vector_Test
{
    class Program
    {
        static int lastDisplayedCount = 0;

        static void Main(string[] args)
        {
            StartListener();

            SetupApiLogic();
                        
            //List<Vector2> VectorList = new List<Vector2>();
            List<Vector2> vectorList = new List<Vector2>();

            //teporarily overriding the generation of seeds and specifying a test seed
            //vectorList = new List<Vector2>();

            vectorList = GenerateSeed();
          
            //vectorList.Add(new Vector2(3, 4));
            //vectorList.Add(new Vector2(3, 5));
            //vectorList.Add(new Vector2(4, 4));
            //teporarily overriding the generation of seeds and specifying a test seed

            var maxSize = DisplayCurrentGrid(vectorList.ToList());

            Console.WriteLine("Above is the generated seed where the game will start");
            Console.WriteLine();
            Console.WriteLine("To Start the game, Press Enter...");
            Console.ReadLine();

            vectorList = StartGame(vectorList.ToList(), maxSize).Item2;

            while (!StartGame(vectorList, maxSize).gameOver)
            {
                vectorList = StartGame(vectorList, maxSize).Item2;
            }

            if (vectorList.Count() > 0)
            {                               
                    DisplayEndGameMessage(2);
                    DisplayCurrentGrid(vectorList);                
            }
            else
            {
                DisplayEndGameMessage(1);
            }

            Console.ReadLine();
        }

        #region API Handlers and Host Logic
        private static void StartListener()
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:5000/");  // Port 0 for dynamic assignment
            listener.Start();

            Task.Run(() => HandleApiRequests(listener));

            // Output the actual listening URL
            Console.WriteLine($"Listening on: {listener.Prefixes.FirstOrDefault()}");
        }

        private static void SetupApiLogic()
        {
            //Register Web API Configuration so that the Angular app can call from
            //this console app as it will act as a server and do all the logic and
            //processing for the game
            var config = new HttpConfiguration();
            WebApiConfig.Register(config);
        }

        static async Task HandleApiRequests(HttpListener listener)
        {
            while (true)
            {
                var context = await listener.GetContextAsync();
                await ProcessRequestAsync(context);
            }
        }

        static async Task ProcessRequestAsync(HttpListenerContext context)
        {
            // Your logic to handle the API request asynchronously
            // ...

            // Send a response
            var response = context.Response;
            var responseString = "API response";
            var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.Close();
        }
        #endregion

        //add unit test to ensure that atleast one living cell is added
        static List<Vector2> GenerateSeed()
        {
            var outList = new List<Vector2>();

            Random r = new Random();
            int seedStart = r.Next(25);

            for (int i = 0; i < seedStart; i++)
            {
                if ((r.NextDouble() > 0.15))
                {
                    var xPos = r.Next(seedStart);
                    var yPos = r.Next(seedStart);

                    while (outList.Any(a => a.X == xPos && a.Y == yPos))
                    {
                        xPos = r.Next(seedStart);
                        yPos = r.Next(seedStart);
                    }

                    Vector2 vector = new Vector2(xPos, yPos);
                    outList.Add(vector);
                }
                else
                {
                    continue;
                }
            }

            return outList;

        }

        public static int DisplayCurrentGrid(List<Vector2> grid)
        {
            lastDisplayedCount = grid.Count();
            int maxX = (int)grid.Max(x => x.X) + 2;
            int maxY = (int)grid.Max(y => y.Y) + 2;

            int maxPoint = maxX > maxY ? maxX : maxY;

            string[,] displayGrid = new string[maxPoint, maxPoint];

            Console.WriteLine();
            for (int i = 0; i < displayGrid.GetLength(0); i++)
            {
                for (int ix = 0; ix < displayGrid.GetLength(1); ix++)
                {
                    var t = grid.Where(a => a.X == i && a.Y == ix).Any();

                    if (!t)
                        Console.Write("0" + " ");
                    else
                        Console.Write("X" + " ");
                }
                Console.WriteLine();
            }

            return maxPoint;


        }

        public static (bool gameOver, List<Vector2>) StartGame(List<Vector2> livingCells, int maxSize)
        {            
            var evolvedGrid = new List<Vector2>();
            var sortedLivingCells = livingCells.OrderBy(v => v.Length()).ToList();   

            for (int i = 0; i < maxSize; i++)
            {
                for (int ix = 0; ix < maxSize; ix++)
                {
                    var isLivingCell = livingCells.Any(a => a.X == i && a.Y == ix);
                    Vector2 testedCell = new Vector2(i, ix);
                    if (!isLivingCell)
                    {

                        if (CheckIfBirth(testedCell, livingCells))
                            evolvedGrid.Add(testedCell);
                    }
                    else
                    {
                        var neighbors = GetNeighbors(testedCell, livingCells);
                        if (CheckLivingCellPersists(neighbors))
                            evolvedGrid.Add(testedCell);
                    }
                }
            }    

            if (evolvedGrid.Count() == 0)
            {
                return (true, new List<Vector2>());
            }
            DisplayCurrentGrid(evolvedGrid);

            return ((evolvedGrid.Count() == livingCells.Count()), evolvedGrid);
        }

        #region RulesCheck

        public static bool CheckIfBirth(Vector2 testedCell, List<Vector2> livingCells)
        {
            var neighbors = GetNeighbors(testedCell, livingCells);

            if (neighbors.Count() == 3)
                return true;
            else
                return false;
        }

        public static bool CheckLivingCellPersists(List<Vector2> neighbors)
        {
            int neighborCount = neighbors.Count();
            if (neighborCount < 2 | neighborCount > 3)
                return false;
            else
                return true;

        }
        #endregion


        #region Neighbor Operations
        static List<Vector2> GetNeighbors(Vector2 targetVector, List<Vector2> vectorList)
        {
            List<Vector2> neighborVectors = new List<Vector2>();

            foreach (var vector in vectorList)
            {
                if (IsNeighbor(targetVector, vector))
                {
                    neighborVectors.Add(vector);
                }
            }

            return neighborVectors;
        }

        static bool IsNeighbor(Vector2 vector1, Vector2 vector2)
        {
            Vector2 difference = vector2 - vector1;

            // Check if directly adjacent or diagonally touching and exclude vectors that are aat the same coordinates
            return vector1 != vector2 &&
              (Math.Abs(difference.X) <= 1 && Math.Abs(difference.Y) <= 1 ||
              Math.Abs(difference.X) <= Math.Sqrt(2) && Math.Abs(difference.Y) <= Math.Sqrt(2));


        }
        #endregion

        public static void DisplayEndGameMessage(int messageIndicator)
        {
           if(messageIndicator == 1)
            {
                Console.WriteLine();
                Console.WriteLine("The game has ended and all cells have died, ALL OUT NUCLEAR WAR HAS ENDED THE WORLD!");
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("The game has now ended, here is the final stage that will not evolve");               
            }
        }


    }


}
