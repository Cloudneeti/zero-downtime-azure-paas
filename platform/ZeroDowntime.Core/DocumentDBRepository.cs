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
    public static class DocumentDBRepository<T> where T : class
    {
        private static DocumentClient client;

        /// <summary>
        /// Method to intialize Cosmos DB
        /// </summary>
        public static void Initialize(string endpoint, string authKey)
        {
            try
            {
                client = new DocumentClient(new Uri(endpoint), authKey);
            }
            catch (DocumentClientException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {

                }
            }
        }

        public static async Task<Document> CreateItemAsync(T item, string DatabaseId, string CollectionId)
        {
            return await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), item);
        }

        public static async Task<IEnumerable<T>> GetItemsAsync(string DatabaseId, string CollectionId)
        {
            IDocumentQuery<T> query = client.CreateDocumentQuery<T>(
                UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId))
                .AsDocumentQuery();

            List<T> results = new List<T>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<T>());
            }

            return results;
        }

    }
}
