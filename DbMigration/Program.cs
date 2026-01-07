using DbMigration;

internal class Program
{
    private static void Main()
    {
        string oldCs = "Host=192.168.10.15;Database=RunnerMotorDB;Username=postgres;Password=142536";
        string newCs = "Host=192.168.10.15;Database=VATPrompt_v2_RunnerDB;Username=postgres;Password=142536";

        //Console.WriteLine($"Tables to migrate: {maps.Count}");

        MigrationEngine.Run(oldCs, newCs);

        Console.WriteLine("Migration completed successfully.");
    }
}