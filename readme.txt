This C# project, called mqttCheck, contains the source of a testing and 
statistical model checking (SMC) tool for MQTT implementations.
It builds upon the property-based testing (PBT) tool FsCheck [1] and provides 
additional properties for SMC. The main feature of the tool is the performance 
analysis (by checking the latency from a client perspective), 
but the tool also supports functional testing.
More details will follow in a yet unpublished article: "How Fast is MQTT?
Statistical Model Checking and Testing of IoT Protocols".

Note that this is an initial version of the source, which will soon be further
cleaned and better documented. 

The entry point to the project is the Tests class, which contains NUnit tests 
that perform:
(1) model-based testing on the system-under-test (SUT) in order to 
produce log-data,
(2) SMC of a timed model with a Monte Carlo simulation, 
(3) SMC of the SUT with the (sequential probability ratio test).
For all these steps, we apply a state machine specification given in the class 
StateMachineSpec, which applies the test-case generation and also the model 
execution. For more details about state machine testing within PBT, see [1].

The project was created with Visual studio 2012 on a Windows Server 2008 R2.
In order to fix the packages of project, the following should be done in 
Visual Studio:
Go to: Tools > NuGet Package Manager > Package Manager Console and click repair
Execute the command: "Update-Package -Reinstall" in this console.


After that the connection to the broker should be configured.
The host name of the MQTT broker can be set in the SUT class. We worked with 
locally installed brokers. The class BrokerHelper provides functions to 
automatically start/stop/reset a local broker. In order to use this functions 
you have to change the path to your local broker installation or just 
uncomment/adjust this functions if you want to test a broker on a different 
machine. Note, we tested the tool primarily with Mosquitto [2] and emqtt [3], 
but we briefly checked functional testing for other MQTT implementations, 
and it might also work.

Then, the project should be build-able and the test should be runnable in the
NUnit 2 test adapter. If the test adapter is not installed automatically, 
it should be installed with the package manager. And you might need to change 
the project architecture for the test explorer:
TEST > Test Settings > Default Process Architecture: x64

Please note the following:
(1)There is a folder LinearRegression that contains regression models and an 
R-script for learning such models from log data.
(2)We use StopWatch ticks for the latency in order to increase the accuracy.
(3)Logs are written per default to /bin/Debug.




[1] https://fscheck.github.io/FsCheck
[2] https://mosquitto.org/
[3] http://emqtt.io/