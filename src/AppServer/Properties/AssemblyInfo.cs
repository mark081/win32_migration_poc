using System.Runtime.CompilerServices;

// Keeps migration implementation types internal while permitting focused contract tests.
[assembly: InternalsVisibleTo("AppServer.FeatureTests")]
