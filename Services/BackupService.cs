using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Task4.model;

public class BackupService
{
    private readonly List<Client> _clients;
    private readonly string _backupFilePath;
    private readonly Timer _timer;

    public BackupService(List<Client> clients, string backupFilePath)
    {
        _clients = clients;
        _backupFilePath = backupFilePath;

        // Set up a timer to trigger the backup every 10 seconds
        _timer = new Timer(BackupData, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
    }

    private void BackupData(object state)
    {
        lock (_clients)
        {
            try
            {
                using (var writer = new StreamWriter(_backupFilePath))
                using (var csv = new CsvWriter(writer, new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)))
                {
                    csv.WriteRecords(_clients);
                }
                Console.WriteLine($"Backup completed at {DateTime.Now}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during backup: {ex.Message}");
            }
        }
    }
}
