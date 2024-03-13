using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

public class QueueService<MsgT>
{
    private readonly QueueClient queueClient;
    private readonly LogBufferService logger;

    public QueueService(
        string queueName,
        IOptions<MyConnectionStrings> _myConnectionStrings,
        LogBufferService logger
    )
    {
        MyConnectionStrings myConnectionStrings = _myConnectionStrings.Value;
        this.logger = logger;

        queueClient = new QueueClient(
            myConnectionStrings.QueueConnectionStr,
            queueName
        );
        queueClient.CreateIfNotExists();
    }

    public async Task<(QueueMessage, MsgT)?> GetMessageAsync()
    {
        QueueMessage[] retrievedMessages = await queueClient.ReceiveMessagesAsync(maxMessages: 1);
        if (retrievedMessages.Length > 0)
        {
            var message = retrievedMessages[0];
            var base64EncodedBytes = Convert.FromBase64String(message.MessageText);
            var decodedMessage = Encoding.UTF8.GetString(base64EncodedBytes);
            MsgT msgObj = JsonConvert.DeserializeObject<MsgT>(decodedMessage);
            return (message, msgObj);
        }

        return null;
    }

    public async Task DeleteMessageAsync(QueueMessage message)
    {
        await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
    }


    public async Task EnqueueMessageAsync(MsgT msgObj)
    {
        string message = JsonConvert.SerializeObject(msgObj);
        if (string.IsNullOrEmpty(message))
            throw new ArgumentNullException(nameof(message));

        var bytes = Encoding.UTF8.GetBytes(message);
        await queueClient.SendMessageAsync(Convert.ToBase64String(bytes));
    }

}

