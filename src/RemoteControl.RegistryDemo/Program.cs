using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RemoteControl.Core.Models;
using RemoteControl.Services.Implementations;
using RemoteControl.Services.Interfaces;

namespace RemoteControl.RegistryDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Remote Control Registry System Demo");
            Console.WriteLine("=====================================\n");

            // Setup dependency injection
            var services = new ServiceCollection()
                .AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information))
                .AddSingleton<IRegistryService, InMemoryRegistryService>()
                .BuildServiceProvider();

            var registry = services.GetRequiredService<IRegistryService>();

            await DemoRegistrySystem(registry);

            Console.WriteLine("\n✅ Demo completed! Press any key to exit...");
            Console.ReadKey();
        }

        static async Task DemoRegistrySystem(IRegistryService registry)
        {
            Console.WriteLine("📋 Step 1: User Registration");
            Console.WriteLine("----------------------------");

            // Register a new user
            var registerRequest = new RegisterRequest
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "password123"
            };

            var registerResponse = await registry.RegisterUserAsync(registerRequest);
            if (registerResponse.Success)
            {
                Console.WriteLine($"✅ User registered: {registerRequest.Username} (ID: {registerResponse.UserId})");
            }
            else
            {
                Console.WriteLine($"❌ Registration failed: {registerResponse.Message}");
                return;
            }

            Console.WriteLine("\n🔐 Step 2: User Login");
            Console.WriteLine("--------------------");

            // Login with the user
            var loginRequest = new LoginRequest
            {
                Username = "testuser",
                Password = "password123"
            };

            var loginResponse = await registry.LoginAsync(loginRequest);
            if (loginResponse.Success)
            {
                Console.WriteLine($"✅ Login successful: {loginResponse.User?.Username}");
                Console.WriteLine($"   Token: {loginResponse.Token?[..20]}...");
            }
            else
            {
                Console.WriteLine($"❌ Login failed: {loginResponse.Message}");
                return;
            }

            Console.WriteLine("\n💻 Step 3: Device Registration");
            Console.WriteLine("-------------------------------");

            // Register a device
            var deviceRequest = new DeviceRegistrationRequest
            {
                UserId = registerResponse.UserId!,
                DeviceName = "My Gaming PC",
                ComputerName = "GAMING-RIG",
                UserName = "TestUser",
                OperatingSystem = "Windows 11"
            };

            var deviceId = await registry.RegisterDeviceAsync(deviceRequest);
            if (!string.IsNullOrEmpty(deviceId))
            {
                Console.WriteLine($"✅ Device registered: {deviceRequest.DeviceName}");
                Console.WriteLine($"   Device ID: {deviceId}");

                // Update device status (simulate agent coming online)
                await registry.UpdateDeviceStatusAsync(deviceId, "192.168.1.100", 7777, true);
                Console.WriteLine($"   Status: Online at 192.168.1.100:7777");
            }
            else
            {
                Console.WriteLine("❌ Device registration failed");
                return;
            }

            Console.WriteLine("\n📱 Step 4: Device Discovery");
            Console.WriteLine("---------------------------");

            // Get user's devices
            var devicesResponse = await registry.GetUserDevicesAsync(registerResponse.UserId!);
            if (devicesResponse.Success)
            {
                Console.WriteLine($"✅ Found {devicesResponse.Devices.Count} device(s):");
                foreach (var device in devicesResponse.Devices)
                {
                    Console.WriteLine($"   🖥️  {device.DeviceId}: {device.DeviceName}");
                    Console.WriteLine($"      Computer: {device.ComputerName}");
                    Console.WriteLine($"      Status: {(device.IsOnline ? "🟢 Online" : "🔴 Offline")}");
                    if (device.IsOnline)
                    {
                        Console.WriteLine($"      Address: {device.CurrentIP}:{device.Port}");
                    }
                }
            }

            Console.WriteLine("\n🔍 Step 5: Device Resolution");
            Console.WriteLine("----------------------------");

            // Demonstrate device ID resolution
            var resolvedDevice = await registry.ResolveDeviceIdAsync(deviceId);
            if (resolvedDevice != null)
            {
                Console.WriteLine($"✅ Device resolved:");
                Console.WriteLine($"   ID: {resolvedDevice.DeviceId}");
                Console.WriteLine($"   Name: {resolvedDevice.DeviceName}");
                Console.WriteLine($"   Address: {resolvedDevice.CurrentIP}:{resolvedDevice.Port}");
                Console.WriteLine($"   Owner: {resolvedDevice.OwnerId}");
            }

            Console.WriteLine("\n🤝 Step 6: Permission Management");
            Console.WriteLine("--------------------------------");

            // Demo user login to test permissions
            var demoLoginResponse = await registry.LoginAsync(new LoginRequest
            {
                Username = "demo",
                Password = "demo123"
            });

            if (demoLoginResponse.Success)
            {
                Console.WriteLine($"✅ Demo user logged in: {demoLoginResponse.User?.Username}");
                
                // Check demo user's devices
                var demoDevicesResponse = await registry.GetUserDevicesAsync(demoLoginResponse.UserId!);
                if (demoDevicesResponse.Success && demoDevicesResponse.Devices.Count > 0)
                {
                    var demoDevice = demoDevicesResponse.Devices[0];
                    Console.WriteLine($"   Demo device: {demoDevice.DeviceId} ({demoDevice.DeviceName})");

                    // Grant permission from demo device to test user
                    var permissionGranted = await registry.GrantPermissionAsync(
                        demoDevice.DeviceId, 
                        registerResponse.UserId!, 
                        PermissionLevel.ViewOnly
                    );

                    if (permissionGranted)
                    {
                        Console.WriteLine($"✅ Permission granted: {registerResponse.User?.Username} can now view {demoDevice.DeviceId}");

                        // Check authorized devices for test user
                        var authorizedDevicesResponse = await registry.GetAuthorizedDevicesAsync(registerResponse.UserId!);
                        if (authorizedDevicesResponse.Success)
                        {
                            Console.WriteLine($"   {registerResponse.User?.Username} can now access {authorizedDevicesResponse.Devices.Count} device(s):");
                            foreach (var authDevice in authorizedDevicesResponse.Devices)
                            {
                                Console.WriteLine($"   📺 {authDevice.DeviceId}: {authDevice.DeviceName} ({(authDevice.OwnerId == registerResponse.UserId ? "Owned" : "Shared")})");
                            }
                        }
                    }
                }
            }

            Console.WriteLine("\n🔎 Step 7: Device Search");
            Console.WriteLine("-----------------------");

            // Search for devices
            var searchResponse = await registry.SearchDevicesAsync("RC-", registerResponse.UserId!);
            if (searchResponse.Success)
            {
                Console.WriteLine($"✅ Search results for 'RC-': {searchResponse.Devices.Count} device(s)");
                foreach (var device in searchResponse.Devices)
                {
                    Console.WriteLine($"   🔍 {device.DeviceId}: {device.DeviceName}");
                }
            }
        }
    }
}
