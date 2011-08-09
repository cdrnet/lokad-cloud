Lokad.Cloud Services App
========================

Implementation of a Cloud Application for cloud services to be run in the Lokad.Cloud AppHost

Use this library to easily build a host programm, NT Service or Azure Worker Role to host Lokad.Cloud services using AppHost. Note that it is also possible to run cloud services directly using ServiceRunner instead of setting up a full AppHost.

App Settings
------------

This application expects a deployment cell settings tag in the following format:

    <Settings>
      <Config name="config-hashSHA256hex" />
      <Services name="settings-hashSHA256hex" />
    </Settings>

Alternatively, instead of referencing external blobs, one or both of the elements can be inlined:

    <Settings>
      <Config>AutofacConfigEncodedInBase64</Config>
      <Services>
        <Service> ... </Service>
        <Service> ... </Service>
      </Services>
    </Settings>

The Autofac IoC config is never touched in string format, yet we recommend to encode it in UTF-8. If it is inlined into the deployment xml, the binary representation is epxected to be encoded as a Base64 string.

The service settings are expected in the following format (UTF-8)

    <Services>
      <Service disabled="false" type="serviceType">
        <ImplementationType typeName="typeName, assemblyName" />
        ... type specific tags ...
      </Service>
      <Service> ... </Service>
    </Services>

Depending on the service type there will be additional elements in the service tag. All these additional settings can be automatically derived from the implementation type and its attributes though, so they are optional (yet override automatically derived settings if set).

Fully defined settings for *queued cloud* services:

    <Service type="QueuedCloudService">
      <ImplementationType typeName="typeName, assemblyName" />
      <Queue name="queueName" />
      <Timing invisibility="00:30:00" continueFor="00:00:30" />
      <Quarantine maxTrials="5" />
    </Service>

Fully defined settings for *scheduled cloud* services:

    <Service type="ScheduledCloudService">
      <ImplementationType typeName="typeName, assemblyName" />
      <Trigger interval="00:05:00" />
    </Service>

Fully defined settings for *scheduled worker* services:

    <Service type="ScheduledWorkerService">
      <ImplementationType typeName="typeName, assemblyName" />
      <Trigger interval="00:05:00" />
    </Service>

Fully defined settings for *daemon* services:

    <Service type="DaemonService">
      <ImplementationType typeName="typeName, assemblyName" />
    </Service>

All the Service-tags can optionally provide an child UserSettings-tag for additional custom implementation-specific settings that will be provided to the service's Initialize method. This is an easy way to parametrize your services further in a deployment-versioned way (as an alternative to the autofac IoC config).