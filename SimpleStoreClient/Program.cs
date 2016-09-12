using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleStoreClient
{
    using System.Fabric;
    using System.ServiceModel;

    using Common;

    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Communication.Client;
    using Microsoft.ServiceFabric.Services.Communication.Wcf.Client;

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var serviceName = new Uri("fabric:/SerFab1/ShoppingCartService");
                var serviceResolver = ServicePartitionResolver.GetDefault();
                var binding = CreateClientConnectionBinding();
                var shoppingClient = new Client(new WcfCommunicationClientFactory<IShoppingCartService>(binding, null, serviceResolver), serviceName);
                Console.WriteLine("Adding item");
                shoppingClient.AddItem(new ShoppingCartItem
                {
                    Amount = 2,
                    UnitPrice = 329.0,
                    ProductName = "XBOX ONE"
                }).Wait();
                Console.WriteLine("Reading items");
                var list = shoppingClient.GetItems().Result;
                list.Select(x => $"{x.ProductName}: {x.UnitPrice} x {x.Amount} = {x.LineTotal}").ToList().ForEach(Console.WriteLine);
                Console.ReadKey();
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        private static NetTcpBinding CreateClientConnectionBinding()
        {
            var binding = new NetTcpBinding
            {
                SendTimeout = TimeSpan.MaxValue,
                ReceiveTimeout = TimeSpan.MaxValue,
                OpenTimeout = TimeSpan.FromSeconds(5),
                CloseTimeout = TimeSpan.FromSeconds(5),
                MaxConnections = int.MaxValue,
                MaxReceivedMessageSize = 1024 * 1024
            };

            binding.MaxBufferSize = (int)binding.MaxReceivedMessageSize;
            binding.MaxBufferPoolSize = Environment.ProcessorCount * binding.MaxReceivedMessageSize;

            return binding;
        }
    }

    public class Client : ServicePartitionClient<WcfCommunicationClient<IShoppingCartService>>, IShoppingCartService
    {
        public Client(WcfCommunicationClientFactory<IShoppingCartService> clientFactory, Uri serviceName ): base(clientFactory, serviceName, new ServicePartitionKey(1))
        {
        }

        public Task AddItem(ShoppingCartItem item)
        {
            return this.InvokeWithRetryAsync(client => client.Channel.AddItem(item));
        }

        public Task DeleteItem(string productName)
        {
            return this.InvokeWithRetryAsync(client => client.Channel.DeleteItem(productName));
        }

        public Task<List<ShoppingCartItem>> GetItems()
        {
            return this.InvokeWithRetryAsync(client => client.Channel.GetItems());
        }
    }
}
