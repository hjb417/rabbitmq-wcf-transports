# RabbitMQ transports for WCF
 
Supported Bindings
 
 - [Task Queues (Work Queues)](#task-queues)
 
##Task Queues
 
Task queues use the **RabbitMQTaskQueueBinding** binding and is based of the [Work Queues](https://www.rabbitmq.com/tutorials/tutorial-two-dotnet.html) example but have the following difference(s).
 
 - Messages are acknowledged as soon as they're deserialized to a  [System.ServiceModel.Channels.Message](https://msdn.microsoft.com/en-us/library/system.servicemodel.channels.message.aspx)
 - A load balancer/throttle can be used to prevent processing of messages on the RabbitMQ queues.
 
 The **RabbitMQTaskQueueBinding** binding supports both session and sessionless services and callback contracts can be uses with sessions. Load balancing is performed by calling the *IDequeueThrottler.Throttle* method before dequeuing a message. The method doesn't need to return immediately and is given a cancellation token when the method need the call has timed out or the channel needs to be closed.
 
The bindings can be created either in configuration or in code. Below is a sample server and client config
```xml
    <?xml version="1.0" encoding="utf-8" ?>
    <configuration>
    <system.serviceModel>
    <extensions>
      <bindingExtensions>
        <add name="rabbitMQTaskQueue" type="HB.RabbitMQ.ServiceModel.TaskQueue.RabbitMQTaskQueueBindingElementCollection, HB.RabbitMQ.ServiceModel"/>
      </bindingExtensions>
    </extensions>
    <bindings>
      <rabbitMQTaskQueue>
        <binding hostName="localhost"/>
      </rabbitMQTaskQueue>
    </bindings>
    <services>
      <service name="ConsoleServer.SimpleService">
        <endpoint address="hb.rmqtq://Contracts.ISimpleService/" binding="rabbitMQTaskQueue" contract="Contracts.ISimpleService"/>
      </service>
    </services>
</system.serviceModel></configurarion>
```
 
```xml
    <?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <system.serviceModel>
    <extensions>
      <bindingExtensions>
        <add name="rabbitMQTaskQueue" type="HB.RabbitMQ.ServiceModel.TaskQueue.RabbitMQTaskQueueBindingElementCollection, HB.RabbitMQ.ServiceModel"/>
      </bindingExtensions>
    </extensions>
    <bindings>
      <rabbitMQTaskQueue>
        <binding hostName="localhost"/>
      </rabbitMQTaskQueue>
    </bindings>
    <client>
      <endpoint name="SimpleService" address="hb.rmqtq://Contracts.ISimpleService/" binding="rabbitMQTaskQueue" contract="Contracts.ISimpleService" />
    </client>
  </system.serviceModel>
</configuration>
```
 
The URI for the service is in the following format. hb.rmqtq://QUEUE_NAME where *QUEUE_NAME* is the name of the queue that the WCF services will read from. The following settings can be passed in query string entries.
 - exchange: The name of the rabbit MQ exchange the queue will be created on.
 - durable: Sets if the queue will be created as durable. The default value is **true**.
 - deleteonclose: Set to true to delete the queue when the channel is closed. The default value is **false**.
 - ttl: A timespan that sets the [queue ttl](https://www.rabbitmq.com/ttl.html#queue-ttl). The default value is **null** which indicates a ttl will not be set on the queue.
