using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Worker;

var saveDir = "../tmp";

var factory = new ConnectionFactory { HostName = "localhost"};
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();
channel.QueueDeclare(queue: "task_queue",
        durable: true,
        exclusive: false,
        autoDelete: false,
        arguments: null);
channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

var consumer = new EventingBasicConsumer(channel);
consumer.Received += (sender, ea) =>
    {
        var filename = Encoding.UTF8.GetString(ea.Body.ToArray());
        var filePath = Path.Combine(saveDir, filename);
        var statusFilePath = Path.Combine(saveDir, $"{Path.GetFileNameWithoutExtension(filename)}_status.txt");
        var resultFilePath = Path.Combine(saveDir, $"{Path.GetFileNameWithoutExtension(filename)}_result.txt");
        var errorFilePath = Path.Combine(saveDir, $"{Path.GetFileNameWithoutExtension(filename)}_error.txt");

        // change status
        using (var fs = new FileStream(statusFilePath, FileMode.Create, FileAccess.Write))
        {
            fs.Write(Encoding.UTF8.GetBytes("Processing"));
        }

        // calc caption
        var result = Runner.GetCaption(filePath);

        // delete image
        File.Delete(filePath);

        // save status and result
        if (result.Error != "")
        {
            using (var fs = new FileStream(statusFilePath, FileMode.Create, FileAccess.Write))
            {
                fs.Write(Encoding.UTF8.GetBytes("Failed"));
            }
            
            using (var fs = new FileStream(errorFilePath, FileMode.Create, FileAccess.Write))
            {
                fs.Write(Encoding.UTF8.GetBytes(result.Error));
            }
        }
        else
        {
            using (var fs = new FileStream(statusFilePath, FileMode.Create, FileAccess.Write))
            {
                fs.Write(Encoding.UTF8.GetBytes("Successful"));
            }

            using (var fs = new FileStream(resultFilePath, FileMode.Create, FileAccess.Write))
            {
                fs.Write(Encoding.UTF8.GetBytes(result.Output));
            }
        }
    };

    while (!channel.IsClosed)
    {
        channel.BasicConsume(queue: "task_queue",
            autoAck: true,
            consumer: consumer);
    }
