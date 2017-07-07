
setlocal

@rem enter this directory
cd /d %~dp0

set TOOLS_PATH=packages\Grpc.Tools.1.0.1\tools\windows_x64

%TOOLS_PATH%\protoc.exe Greeter\protos\greeter.proto --csharp_out Greeter 

%TOOLS_PATH%\protoc.exe Greeter\protos\greeter.proto --grpc_out Greeter --plugin=protoc-gen-grpc=%TOOLS_PATH%\grpc_csharp_plugin.exe

endlocal
