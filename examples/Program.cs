using System;
using System.Threading.Tasks;

namespace MBClient.CS.Examples
{
    /// <summary>
    /// The main entry point for the examples, allowing a user to select and run individual examples.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Asynchronously runs the selected example based on user input.
        /// </summary>
        /// <param name="args">Command-line arguments (not used).</param>
        static async Task Main(string[] args)
        {
            Console.WriteLine("Choose an example to run:");
            Console.WriteLine("1. Listener");
            Console.WriteLine("2. Requester");
            Console.WriteLine("3. Responder");
            Console.WriteLine("4. Sort Client");
            Console.WriteLine("5. All");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    // Run the Listener example
                    await Listener.Run();
                    break;
                case "2":
                    // Run the Requester example
                    await Requester.Run();
                    break;
                case "3":
                    // Run the Responder example
                    await Responder.Run();
                    break;
                case "4":
                    // Run the SortClient example
                    await SortClient.Run();
                    break;
                case "5":
                    // Run all examples concurrently
                    await Task.WhenAll(
                        Listener.Run(),
                        Requester.Run(),
                        Responder.Run(),
                        SortClient.Run()
                    );
                    break;
                default:
                    Console.WriteLine("Invalid choice.");
                    return;
            }

            // Keep the application running to allow background tasks to complete
            await Task.Delay(-1);
        }
    }
}
