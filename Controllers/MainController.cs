using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Task4.model;

namespace Task4.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MainController : ControllerBase
    {
        private static List<Client> _clients = new List<Client>();
        private static string fullPath = Path.Combine("Files", "clients.csv");
        private List<Client> existingClients;
        private BackupService _backupService;

        public MainController()
        {
            // Read existing data from the CSV file

            if (System.IO.File.Exists(fullPath))
            {
                using (var reader = new StreamReader(fullPath))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    existingClients = csv.GetRecords<Client>().ToList();
                }
            }
            else
            {
                existingClients = new List<Client>();
            }
            _backupService = new BackupService(existingClients, Path.Combine("Files", "backup.csv"));

        }

        [HttpPost("CreateClient")]
        public IActionResult CreateClient([FromForm] ClientDto clientDto)
        {
            try
            {
                // Validate input data (you can add more validation as needed)
                if (string.IsNullOrEmpty(clientDto.Name) || clientDto.Salary <= 0)
                {
                    return BadRequest("Invalid input data");
                }

                // Create a new client
                var client = new Client
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = clientDto.Name,
                    Salary = clientDto.Salary,
                    Balance = 0, // Assuming initial balance is zero
                    CreationDate = DateTime.Now,
                    IsDeleted = false
                };

                // Add the client to the list
                _clients.Add(client);

                // Save data to CSV (you can call this method based on your requirements)
                SaveDataToCsv();

                return Ok(client);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal Server Error");
            }
        }

        [HttpDelete("DeleteClient")]
        public IActionResult DeleteClient([FromQuery] string id)
        {
            try
            {
                // Find the client by ID
                var clientToDelete = existingClients.FirstOrDefault(c => c.Id.Equals(id));

                // Check if the client exists
                if (clientToDelete != null)
                {
                    // Mark the client as deleted
                    clientToDelete.IsDeleted = true;

                    // Save the updated data to the CSV file
                    SaveDataToCsv();

                    return Ok($"Client with ID {id} marked as deleted.");
                }
                else
                {
                    return NotFound($"Client with ID {id} not found.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal Server Error");
            }
        }

        [HttpPost("Deposit")]
        public IActionResult Deposit([FromForm] DepositDto depositDto)
        {
            try
            {
                // Validate input data
                if (string.IsNullOrEmpty(depositDto.Id) || depositDto.Amount <= 0)
                {
                    return BadRequest("Invalid input data");
                }

                // Find the client by ID
                var clientToDeposit = existingClients.FirstOrDefault(c => c.Id.Equals(depositDto.Id));

                // Check if the client exists
                if (clientToDeposit != null && !clientToDeposit.IsDeleted)
                {
                    // Deposit funds into the client's account
                    clientToDeposit.Balance += depositDto.Amount;

                    // Save the updated data to the CSV file
                    SaveDataToCsv();

                    return Ok($"Successfully deposited {depositDto.Amount} into the account of client with ID {depositDto.Id}. New balance: {clientToDeposit.Balance}");
                }
                else
                {
                    return NotFound($"Client with ID {depositDto.Id} not found or marked as deleted.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal Server Error");
            }
        }


        [HttpPost("Withdraw")]
        public IActionResult Withdraw([FromForm] WithdrawDto withdrawDto)
        {
            try
            {
                // Validate input data
                if (string.IsNullOrEmpty(withdrawDto.Id) || withdrawDto.Amount <= 0)
                {
                    return BadRequest("Invalid input data");
                }

                // Find the client by ID
                var clientToWithdraw = existingClients.FirstOrDefault(c => c.Id.Equals(withdrawDto.Id));

                // Check if the client exists
                if (clientToWithdraw != null && !clientToWithdraw.IsDeleted)
                {
                    // Check if the client has sufficient funds
                    if (clientToWithdraw.Balance >= withdrawDto.Amount)
                    {
                        // Withdraw funds from the client's account
                        clientToWithdraw.Balance -= withdrawDto.Amount;

                        // Save the updated data to the CSV file
                        SaveDataToCsv();

                        return Ok($"Successfully withdrew {withdrawDto.Amount} from the account of client with ID {withdrawDto.Id}. New balance: {clientToWithdraw.Balance}");
                    }
                    else
                    {
                        return BadRequest($"Insufficient funds for withdrawal. Current balance: {clientToWithdraw.Balance}");
                    }
                }
                else
                {
                    return NotFound($"Client with ID {withdrawDto.Id} not found or marked as deleted.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal Server Error");
            }
        }


        [HttpPost("Transfer")]
        public IActionResult Transfer([FromForm] TransferDto transferDto)
        {
            try
            {
                // Validate input data
                if (string.IsNullOrEmpty(transferDto.SenderId) || string.IsNullOrEmpty(transferDto.ReceiverId) || transferDto.Amount <= 0)
                {
                    return BadRequest("Invalid input data");
                }

                // Find the sender and receiver clients by ID
                var senderClient = existingClients.FirstOrDefault(c => c.Id.Equals(transferDto.SenderId));
                var receiverClient = existingClients.FirstOrDefault(c => c.Id.Equals(transferDto.ReceiverId));

                // Check if both clients exist and are not deleted
                if (senderClient != null && receiverClient != null && !senderClient.IsDeleted && !receiverClient.IsDeleted)
                {
                    // Check if the sender has sufficient funds
                    if (senderClient.Balance >= transferDto.Amount)
                    {
                        // Perform the transfer
                        senderClient.Balance -= transferDto.Amount;
                        receiverClient.Balance += transferDto.Amount;

                        // Save the updated data to the CSV file
                        SaveDataToCsv();

                        return Ok($"Successfully transferred {transferDto.Amount} from the account of client with ID {transferDto.SenderId} to the account of client with ID {transferDto.ReceiverId}. Sender's new balance: {senderClient.Balance}, Receiver's new balance: {receiverClient.Balance}");
                    }
                    else
                    {
                        return BadRequest($"Insufficient funds for transfer. Sender's current balance: {senderClient.Balance}");
                    }
                }
                else
                {
                    if (senderClient != null && senderClient.IsDeleted)
                    {
                        return BadRequest($"Transfer not allowed. Sender account with ID {transferDto.SenderId} is marked as deleted.");
                    }
                    else if (receiverClient != null && receiverClient.IsDeleted)
                    {
                        return BadRequest($"Transfer not allowed. Receiver account with ID {transferDto.ReceiverId} is marked as deleted.");
                    }
                    else
                    {
                        return NotFound($"One or both clients not found. Sender ID: {transferDto.SenderId}, Receiver ID: {transferDto.ReceiverId}");
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal Server Error");
            }
        }


        /// <summary>
        /// Helper function
        /// </summary>
        // Save data to CSV file
        private void SaveDataToCsv()
        {
            lock (existingClients)
            {
                // Combine the existing clients with the new clients
                var updatedClients = existingClients.Concat(_clients).ToList();

                // Write the updated list back to the CSV file
                using (var writer = new StreamWriter(fullPath))
                using (var csv = new CsvWriter(writer, new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)))
                {
                    csv.WriteRecords(updatedClients);
                }
            }
        }
    }
    }
