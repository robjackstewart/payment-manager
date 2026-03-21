namespace PaymentManager.Host.Local.Infrastructure;

/// <summary>
/// Public marker type used as <c>TEntryPoint</c> for
/// <c>DistributedApplicationTestingBuilder.CreateAsync&lt;InfrastructureAppHost&gt;()</c>.
/// The top-level entry point in <c>AppHost.cs</c> is <c>internal</c>, so this class
/// provides a stable public reference into the Infrastructure assembly.
/// </summary>
public sealed class InfrastructureAppHost;
