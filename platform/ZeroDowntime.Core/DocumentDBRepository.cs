using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ZeroDowntime.Core
{
    public static class DocumentDBRepository
    {
        private static DocumentClient client;

        private static readonly string DatabaseId = "NBMEDB";
        private static readonly string CollectionId = "Users";
        /// <summary>
        /// Method to intialize Cosmos DB
        /// </summary>
        public static void Initialize(string endpoint, string authKey)
        {
            try
            {
                client = new DocumentClient(new Uri(endpoint), authKey);
                CreateDatabaseIfNotExistsAsync().Wait();
                CreateCollectionIfNotExistsAsync().Wait();

            }
            catch (DocumentClientException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {

                }
            }
        }

        private static async Task CreateDatabaseIfNotExistsAsync()
        {
            try
            {
                await client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(DocumentDBRepository.DatabaseId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await client.CreateDatabaseAsync(new Database { Id = DocumentDBRepository.DatabaseId });
                }
                else
                {
                    throw;
                }
            }
        }

        private static async Task CreateCollectionIfNotExistsAsync()
        {
            try
            {
                await client.ReadDocumentCollectionAsync(
                    UriFactory.CreateDocumentCollectionUri(DocumentDBRepository.DatabaseId, 
                    DocumentDBRepository.CollectionId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(DocumentDBRepository.DatabaseId),
                        new DocumentCollection { Id = DocumentDBRepository.CollectionId },
                        new RequestOptions { OfferThroughput = 1000 });
                }
                else
                {
                    throw;
                }
            }
        }

        public static async Task<Document> CreateUserAsync(NBMEUser user)
        {
            return await client.CreateDocumentAsync(
                UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), user);
        }

        public static async Task<IEnumerable<NBMEUser>> GetUsersAsync()
        {
            IDocumentQuery<NBMEUser> query = client.CreateDocumentQuery<NBMEUser>(
                UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId))
                .AsDocumentQuery();

            List<NBMEUser> results = new List<NBMEUser>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<NBMEUser>());
            }

            return results;
        }

        public static async Task<NBMEUser> GetItemAsync(string docLink)
        {
            return await client.ReadDocumentAsync<NBMEUser>(docLink);
        }

        public static async Task UpsertNbmeUser(NBMEUser user)
        {
            await client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), user);
        }

    }
}
