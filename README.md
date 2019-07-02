# Theory of operation
* Hypervisor application listens for messages and maintans a collection of registered application instances to monitor.
  * If no message from an instance within the delinquency time for that instance then trigger instance failure.
  * Upon trigger kills previous instance (if found) and starts a new instance.
    * if an instance is found and killed then delay 5s before start new instance.
      * Provides ample time for previous killed instance and its handles to fully dispose.
  * Start/restarts are minimum 30s apart.
    * Helps prevent overload and interference between instances.
* Client application run a loop and send a message to hypervisor every interval.
  * Suggest 5 second intervals and delinquency of 90s, however delinquency time heavily dependent upon gestalt
    * For basic needs a static 5s/90s will be good enough.
    * Some applications may warrant a variable delinquency based on the current state of the instance.  



## Message members
* `string` `AppName` `C2S`, the name to use with the console display, aesthetic only
* `int` `DelinquencyTime` `C2S`, the amount of time (in seconds) since the last message before a failure is triggered
* `bool` `DecalInject` `C2S`, whether or not to inject decal upon start/restart
  * registration fail if decal's `Inject.dll` file can't be found 
* `string` `CmdLine` `C2S`, command line to use, *automatically detected* upon message creation
* `string` `ExePath` `C2S`, path to the executable *automatically detected* upon message creation
* `int` `ProcessId` `C2S`, the current `PID`, *automatically detected* upon message creation
* `TriggerType` `Trigger` `C2S`, only `canary` for now, *automatically assigned* upon message creation
* `int` `PoolSize` `S2C`, returned count of the current collection of registered application instances

## Auto message construction
```CSharp
public static AppMessage New(string appName, int delinquencyTime, bool DecalInject)
```

## Usage:
```CSharp
    AppMessage resp = Client.Send(AppMessage.New($"MyPlugin for {Core.CharacterFilter.AccountName}", 90, true));
    if (!WatchDogRegistered && resp != null)
    {
        WatchDogRegistered = true;
        Log($"Registered with watchdog, pool occupancy: {resp.PoolSize}");
    }
```
