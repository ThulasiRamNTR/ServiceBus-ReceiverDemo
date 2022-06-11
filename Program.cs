using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QueueReceiver
{
    internal class Program
    {
        // connection string to your Service Bus namespace
        private static readonly String connectionString = "";

        // name of your Service Bus queue
        private static readonly String queueName = "";


        // the client that owns the connection and can be used to create senders and receivers
        private static ServiceBusClient client;

        //the client that owns the connection and can be used to receive messages from queue/topic
        private static ServiceBusReceiver receiver;

        //ServiceBus processor, that processes the messages concurrently as configured through options
        private static ServiceBusProcessor processor;

        private static ConcurrentAsyncAwaitRunner concurrentAsyncAwaitRunner;

        //Flags to identify the type of approach to be used
        private static Boolean isProccesorApproach;
        private static Boolean isReceiverApproach;

        // handle received messages
        private static async Task MessageHandlerAsync(ProcessMessageEventArgs args)
        {
            ServiceBusLeaseObject serviceBusLeaseObject = new ServiceBusLeaseObject(receiver, true); //Note: This is useful only in receiverApproach
            serviceBusLeaseObject.ServiceBusReceivedMessage = args.Message;

            String body = args.Message.Body.ToString();
            Console.WriteLine($"Received: {body}");

            Thread.Sleep(30000); //do your processing

            serviceBusLeaseObject.Dispose();

            // complete the message. messages is deleted from the queue. 
            await args.CompleteMessageAsync(args.Message);
            // release the semaphoreSlim for next execution to take place
            Task task = Task.Run(() => ReleaseAsync(), new CancellationTokenSource().Token); //Note: This is useful only in receiverApproach


        }

        // handle any errors when receiving messages
        private static Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine(args.Exception.ToString());
            return Task.CompletedTask;
        }

        public static void Main()
        {
            ReceiverAsync().GetAwaiter().GetResult();
            Console.WriteLine("Press any key to end the application");
            Console.ReadKey();
        }

        private static async Task ReceiverAsync()
        {
            await Task.Delay(0);
            // The Service Bus client types are safe to cache and use as a singleton for the lifetime
            // of the application, which is best practice when messages are being published or read
            // regularly.
            //

            // Create the client object that will be used to create sender and receiver objects
            client = new ServiceBusClient(connectionString);
            //var queueClient = new QueueClient(connectionString, queueName);


            if (isProccesorApproach) 
            { 
                //Prototype-1: Competing Consumer:

                //Prototype-2: Using processor
                //Issues: couldn't renewLocks for when heavy processing work needs to happen with message
                //      we can only use the default retry options upto some given time in ClientOptions, which internally takes care of renew lock
                // Pros:
                //      can handle concurrently processing the messages to the defined concurrency value

            
                // create a processor that we can use to process the messages
                ServiceBusProcessorOptions serviceBusProcessorOptions = new ServiceBusProcessorOptions();
                serviceBusProcessorOptions.MaxConcurrentCalls = 5;
                processor = client.CreateProcessor(queueName, serviceBusProcessorOptions);

                try
                {
                    // add handler to process messages
                    processor.ProcessMessageAsync += MessageHandlerAsync;

                    // add handler to process any errors
                    processor.ProcessErrorAsync += ErrorHandler;

                    // start processing 
                    await processor.StartProcessingAsync();

                    Console.WriteLine("Listener Started and then press any key to end the processing");
                    Console.ReadKey();

                    // stop processing 
                    Console.WriteLine("\nStopping the receiver...");
                    await processor.StopProcessingAsync();
                    Console.WriteLine("Stopped receiving messages");
                }
                finally
                {
                    // Calling DisposeAsync on client types is required to ensure that network
                    // resources and other unmanaged objects are properly cleaned up.
                    await processor.DisposeAsync();
                    await client.DisposeAsync();
                }
            }

            if (isReceiverApproach)
            {
                //Prototype-3: Receiver approach
                // Pros: can renewLocks , so we can leverage those to create some LeaseObjects to internally have timer and during elapse 
                //      automatically renew the lock , and during expiry dispose the leaseObject
                // Cons: 
                //      cannot receive message concurrently, so we need to do some ConcurrentAsyncAwaitRunner, which can keep on
                //      getting messages into it's queue, and as message arrives, the SemaPhore slim
                //      can start processing the messages when resource is available, and when processing is done, semaphoreSlim can be 
                //      released and the next concurrent thread can be fired(which receives the next message for us)

                ServiceBusReceiverOptions serviceBusReceiverOptions = new ServiceBusReceiverOptions();
                serviceBusReceiverOptions.ReceiveMode = ServiceBusReceiveMode.PeekLock;
                receiver = client.CreateReceiver(queueName, serviceBusReceiverOptions);

                //ServiceBusReceivedMessage serviceBusReceivedMessage = await serviceBusReceiver.ReceiveMessageAsync();
                concurrentAsyncAwaitRunner = new ConcurrentAsyncAwaitRunner(3);
                concurrentAsyncAwaitRunner.RunTask += HandleReceiveMessageAsync;
                await concurrentAsyncAwaitRunner.ExecuteAsync();

                //serviceBusReceivedMessage.
                //serviceBusReceiver.
                //Prototype-4: MessageHandler and HandlerOptions
                //https://markheath.net/post/azure-service-bus-messaging-6
                //sad part: this dll version Microsoft.Azure.ServiceBus is deprecated


                //QueueClient queueClient = QueueClient.CreateFromConnectionString(connectionString);
                //MessageHandlerOptions options = new 
            }
        }

        private static async Task HandleReceiveMessageAsync(RunTaskEventArgs runTaskEventArgs)
        {
            //max messages is the available room
            Int32 maxMessages = runTaskEventArgs.AvailableRoom;

            //receive the messages from servicebus queue/topic and prepare ProcessMessageEventArgs
            IReadOnlyList<ServiceBusReceivedMessage> serviceBusReceivedMessages = await receiver.ReceiveMessagesAsync(maxMessages);
            foreach (ServiceBusReceivedMessage serviceBusReceivedMessage in serviceBusReceivedMessages)
            {
                ProcessMessageEventArgs processMessageEventArgs = new ProcessMessageEventArgs(serviceBusReceivedMessage, receiver, new CancellationTokenSource().Token);
                Func<ProcessMessageEventArgs, Task> processAction = MessageHandlerAsync;
                Task task = Task.Run(() => MessageHandlerAsync(processMessageEventArgs), new CancellationTokenSource().Token);
                //for each MessageHandler , handle lock expiry exceptions and close objects and release semaphore accordingly

            }

        }

        private static async Task ReleaseAsync()
        {
            await concurrentAsyncAwaitRunner.ReleaseAsync();
        }
    }
}