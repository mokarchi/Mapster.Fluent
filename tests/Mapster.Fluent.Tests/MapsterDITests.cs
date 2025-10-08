using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Reflection;

namespace Mapster.Fluent.Tests
{
    [TestClass]
    public class MapsterDITests
    {
        [TestMethod]
        public void AddMapster_BackwardCompatibility_ShouldStillWork()
        {
            // Arrange
            var services = new ServiceCollection();
            var config = new TypeAdapterConfig();
            config.NewConfig<TestUser, TestUserDto>()
                .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}");
            services.AddSingleton(config);

            // Act
            services.AddMapster();

            var serviceProvider = services.BuildServiceProvider();
            var mapper = serviceProvider.GetRequiredService<IMapper>();

            // Assert
            mapper.ShouldNotBeNull();
            // Note: Without config parameter, it uses default Mapper, not ServiceMapper
            mapper.ShouldBeOfType<Mapper>();
        }

        // ===== FLUENT CONFIGURATION API TESTS =====

        [TestMethod]
        public void AddMapster_WithFluentConfiguration_RegistersMapper()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMapster(options =>
            {
                options.ConfigureAction = config =>
                {
                    config.ForType<TestUser, TestUserDto>()
                        .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}");
                };
            });

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();

            // Assert
            mapper.ShouldNotBeNull();
            var user = new TestUser { FirstName = "John", LastName = "Doe" };
            var dto = mapper.Map<TestUserDto>(user);
            dto.FullName.ShouldBe("John Doe");
        }

        [TestMethod]
        public void AddMapster_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Should.Throw<ArgumentNullException>(() =>
                services.AddMapster(null));
        }

        [TestMethod]
        public void AddMapster_WithNullServices_ThrowsArgumentNullException()
        {
            // Arrange
            IServiceCollection services = null;

            // Act & Assert
            Should.Throw<ArgumentNullException>(() =>
                services.AddMapster(options => { }));
        }

        [TestMethod]
        public void AddMapster_WithMultipleMappings_ConfiguresAllMappings()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMapster(options =>
            {
                options.ConfigureAction = config =>
                {
                    config.ForType<TestUser, TestUserDto>()
                        .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}");

                    config.ForType<TestProduct, TestProductDto>()
                        .Map(dest => dest.DisplayName, src => src.Name.ToUpper());
                };
            });

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();

            // Assert
            var user = new TestUser { FirstName = "Jane", LastName = "Smith" };
            var userDto = mapper.Map<TestUserDto>(user);
            userDto.FullName.ShouldBe("Jane Smith");

            var product = new TestProduct { Name = "widget" };
            var productDto = mapper.Map<TestProductDto>(product);
            productDto.DisplayName.ShouldBe("WIDGET");
        }

        [TestMethod]
        public void AddMapster_WithNoConfiguration_StillRegistersMapper()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMapster(options => { });

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();

            // Assert
            mapper.ShouldNotBeNull();
        }

        // ===== ASSEMBLY SCANNING TESTS =====

        [TestMethod]
        public void ScanMapster_WithValidAssembly_RegistersIRegisterImplementations()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.ScanMapster(Assembly.GetExecutingAssembly());

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();
            var config = provider.GetRequiredService<TypeAdapterConfig>();

            // Assert
            mapper.ShouldNotBeNull();
            config.ShouldNotBeNull();

            // Test that our IRegister implementation was discovered
            var user = new TestUser { FirstName = "Scanned", LastName = "User" };
            var dto = mapper.Map<TestUserDto>(user);
            dto.FullName.ShouldBe("Scanned User");
        }

        [TestMethod]
        public void ScanMapster_WithNullServices_ThrowsArgumentNullException()
        {
            // Arrange
            IServiceCollection services = null;

            // Act & Assert
            Should.Throw<ArgumentNullException>(() =>
                services.ScanMapster(Assembly.GetExecutingAssembly()));
        }

        [TestMethod]
        public void ScanMapster_WithNullAssemblies_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Should.Throw<ArgumentException>(() =>
                services.ScanMapster(null));
        }

        [TestMethod]
        public void ScanMapster_WithEmptyAssemblies_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Should.Throw<ArgumentException>(() =>
                services.ScanMapster());
        }

        [TestMethod]
        public void AddMapster_WithAssembliesToScan_RegistersIRegisterImplementations()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMapster(options =>
            {
                options.AssembliesToScan = [Assembly.GetExecutingAssembly()];
            });

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();

            // Assert
            var user = new TestUser { FirstName = "Assembly", LastName = "Scanned" };
            var dto = mapper.Map<TestUserDto>(user);
            dto.FullName.ShouldBe("Assembly Scanned");
        }

        [TestMethod]
        public void AddMapster_WithMultipleAssemblies_ScansAllAssemblies()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMapster(options =>
            {
                options.AssembliesToScan =
                [
                    Assembly.GetExecutingAssembly(),
                    typeof(TypeAdapterConfig).Assembly
                ];
            });

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();

            // Assert
            mapper.ShouldNotBeNull();
        }

        [TestMethod]
        public void AddMapster_WithScanningAndConfiguration_AppliesBoth()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMapster(options =>
            {
                options.AssembliesToScan = [Assembly.GetExecutingAssembly()];
                options.ConfigureAction = config =>
                {
                    config.ForType<TestProduct, TestProductDto>()
                        .Map(dest => dest.DisplayName, src => $"Product: {src.Name}");
                };
            });

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();

            // Assert - Test scanned mapping
            var user = new TestUser { FirstName = "Both", LastName = "Applied" };
            var userDto = mapper.Map<TestUserDto>(user);
            userDto.FullName.ShouldBe("Both Applied");

            // Assert - Test additional configuration
            var product = new TestProduct { Name = "Test" };
            var productDto = mapper.Map<TestProductDto>(product);
            productDto.DisplayName.ShouldBe("Product: Test");
        }

        // ===== SERVICE MAPPER BY DEFAULT TESTS =====

        [TestMethod]
        public void AddMapster_DefaultsToServiceMapper()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMapster(options =>
            {
                options.ConfigureAction = config =>
                {
                    config.ForType<TestUser, TestUserDto>();
                };
            });

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();

            // Assert
            mapper.ShouldNotBeNull();
            mapper.ShouldBeOfType<ServiceMapper>();
        }

        [TestMethod]
        public void AddMapster_WithUseServiceMapperTrue_RegistersServiceMapper()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMapster(options =>
            {
                options.UseServiceMapper = true;
                options.ConfigureAction = config =>
                {
                    config.ForType<TestUser, TestUserDto>();
                };
            });

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();

            // Assert
            mapper.ShouldNotBeNull();
            mapper.ShouldBeOfType<ServiceMapper>();
        }

        [TestMethod]
        public void AddMapster_WithUseServiceMapperFalse_RegistersBasicMapper()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMapster(options =>
            {
                options.UseServiceMapper = false;
                options.ConfigureAction = config =>
                {
                    config.ForType<TestUser, TestUserDto>();
                };
            });

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();

            // Assert
            mapper.ShouldNotBeNull();
            mapper.ShouldBeOfType<Mapper>();
        }

        [TestMethod]
        public void AddMapster_ServiceMapper_IsScopedPerRequest()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddMapster(options =>
            {
                options.UseServiceMapper = true;
            });

            var provider = services.BuildServiceProvider();

            // Act
            IMapper mapper1, mapper2, mapper3;
            using (var scope1 = provider.CreateScope())
            {
                mapper1 = scope1.ServiceProvider.GetRequiredService<IMapper>();
            }

            using (var scope2 = provider.CreateScope())
            {
                mapper2 = scope2.ServiceProvider.GetRequiredService<IMapper>();
            }

            using (var scope3 = provider.CreateScope())
            {
                mapper3 = scope3.ServiceProvider.GetRequiredService<IMapper>();
            }

            // Assert - Each scope gets a different ServiceMapper instance
            mapper1.ShouldNotBeSameAs(mapper2);
            mapper2.ShouldNotBeSameAs(mapper3);
            mapper1.ShouldNotBeSameAs(mapper3);
        }

        [TestMethod]
        public void AddMapster_RegistersTypeAdapterConfig()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMapster(options =>
            {
                options.ConfigureAction = config =>
                {
                    config.ForType<TestUser, TestUserDto>();
                };
            });

            var provider = services.BuildServiceProvider();
            var config = provider.GetRequiredService<TypeAdapterConfig>();

            // Assert
            config.ShouldNotBeNull();
        }

        [TestMethod]
        public void AddMapster_TypeAdapterConfig_IsSingleton()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddMapster(options => { });

            var provider = services.BuildServiceProvider();

            // Act
            TypeAdapterConfig config1, config2;
            using (var scope1 = provider.CreateScope())
            {
                config1 = scope1.ServiceProvider.GetRequiredService<TypeAdapterConfig>();
            }

            using (var scope2 = provider.CreateScope())
            {
                config2 = scope2.ServiceProvider.GetRequiredService<TypeAdapterConfig>();
            }

            // Assert - Same config instance across scopes
            config1.ShouldBeSameAs(config2);
        }

        // ===== INTEGRATION TESTS =====

        [TestMethod]
        public void Integration_FluentConfiguration_ServiceMapper_AssemblyScanning_WorkTogether()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act - Use all three features together
            services.AddMapster(options =>
            {
                // 1. Fluent Configuration
                options.ConfigureAction = config =>
                {
                    config.ForType<TestProduct, TestProductDto>()
                        .Map(dest => dest.DisplayName, src => $"[{src.Name}]");
                };

                // 2. Assembly Scanning
                options.AssembliesToScan = [Assembly.GetExecutingAssembly()];

                // 3. ServiceMapper by Default
                options.UseServiceMapper = true;
            });

            var provider = services.BuildServiceProvider();

            // Assert
            using var scope = provider.CreateScope();
            var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

            // Test scanned mapping (from IRegister)
            var user = new TestUser { FirstName = "Integration", LastName = "Test" };
            var userDto = mapper.Map<TestUserDto>(user);
            userDto.FullName.ShouldBe("Integration Test");

            // Test fluent configuration
            var product = new TestProduct { Name = "Widget" };
            var productDto = mapper.Map<TestProductDto>(product);
            productDto.DisplayName.ShouldBe("[Widget]");

            // Test ServiceMapper type
            mapper.ShouldBeOfType<ServiceMapper>();
        }

        [TestMethod]
        public void ScanMapster_UsesServiceMapperByDefault()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.ScanMapster(Assembly.GetExecutingAssembly());

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();

            // Assert
            mapper.ShouldBeOfType<ServiceMapper>();
        }
    }

    // ===== TEST MODELS =====

    public class TestUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class TestUserDto
    {
        public string FullName { get; set; }
    }

    public class TestProduct
    {
        public string Name { get; set; }
    }

    public class TestProductDto
    {
        public string DisplayName { get; set; }
    }

    // ===== TEST IREGISTER IMPLEMENTATION =====

    public class TestUserMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.ForType<TestUser, TestUserDto>()
                .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}");
        }
    }
}
