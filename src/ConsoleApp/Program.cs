using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using BlobResumableUpload.Model;

namespace BlobResumableUpload
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var command = Args.Configuration.Configure<InputArguments>().CreateAndBind(args);

            string dateNow = DateTime.UtcNow.ToString("yyyyMMddHHmm");
            string storageAccountConnectionString = command.ConnectionString;
            string containerName = command.ContainerName;
            string fileName = command.FileName;
            int blockSize = command.BlockSize * 1024;

            var blobContainerClient = new Azure.Storage.Blobs.BlobContainerClient(storageAccountConnectionString, containerName);
            await blobContainerClient.CreateIfNotExistsAsync();
            var blob = new Azure.Storage.Blobs.Specialized.BlockBlobClient(storageAccountConnectionString, containerName, $"{Path.GetFileName(fileName)}");
            var response = default(Azure.Response<BlockList>);

            try
            {
                response = await blob.GetBlockListAsync(BlockListTypes.All);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"**************DON'T CARE!**************\n{ex}**************DON'T CARE!**************");
            }

            using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                //block count is the number of blocks + 1 for the last one
                int blockCount = (int)(fileStream.Length / blockSize) + 1;

                //List of block ids; the blocks will be committed in the order of this list 
                HashSet<string> blockIDs = new HashSet<string>();

                //starting block number
                int blockNumber = 1;

                try
                {
                    int bytesRead = 0; //number of bytes read so far
                    long bytesLeft = fileStream.Length; //number of bytes left to read and upload
                    //do until all of the bytes are uploaded

                    List<Task> taskList = new List<Task>();

                    while (bytesLeft > 0)
                    {
                        int bytesToRead;
                        if (bytesLeft >= blockSize)
                        {
                            //more than one block left, so put up another whole block
                            bytesToRead = blockSize;
                        }
                        else
                        {
                            //less than one block left, read the rest of it
                            bytesToRead = (int)bytesLeft;
                        }

                        //create a blockID from the block number, add it to the block ID list
                        //the block ID is a base64 string
                        string blockId = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes($"BlockId{blockNumber:0000000}"));
                        blockIDs.Add(blockId);

                        //set up new buffer with the right size, and read that many bytes into it 
                        byte[] bytes = new byte[bytesToRead];
                        fileStream.Read(bytes, 0, bytesToRead);

                        //calculate the MD5 hash of the byte array
                        byte[] blockHash = GetMD5HashFromStream(bytes);

                        //upload the block, provide the hash so Azure can verify it
                        if (!(response?.Value?.UncommittedBlocks?.Any(ub => ub.Name == blockId) ?? false))
                        {
                            //await blob.StageBlockAsync(blockId, new MemoryStream(bytes), blockHash).ConfigureAwait(false);
                            taskList.Add(blob.StageBlockAsync(blockId, new MemoryStream(bytes), blockHash));
                            Console.WriteLine($"{blockNumber}/{blockCount} - BlockId: {blockId} sent!");
                        }
                        else
                        {
                            Console.WriteLine($"{blockNumber}/{blockCount} - BlockId: {blockId}");
                        }
                        
                        
                        //increment/decrement counters
                        bytesRead += bytesToRead;
                        bytesLeft -= bytesToRead;
                        blockNumber++;
                    }

                    //commit the blocks
                    await Task.WhenAll(taskList).ConfigureAwait(false);
                    await blob.CommitBlockListAsync(blockIDs);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception thrown = {0}", ex);
                }


                Console.WriteLine("Press any key to continue . . .");
                Console.ReadLine();
            }

        }

        private static byte[] GetMD5HashFromStream(byte[] bytes)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                md5.TransformFinalBlock(bytes, 0, bytes.Length);
                return md5.Hash;
            }
        }
    }
}
