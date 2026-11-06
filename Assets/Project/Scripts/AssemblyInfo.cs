using System.Runtime.CompilerServices;

// The behavior tree editor and the tests edit trees through the authoring API, which
// stays internal so gameplay code can't rewire a shared tree asset at runtime.
[assembly: InternalsVisibleTo("CGD.Editor")]
[assembly: InternalsVisibleTo("CGD.Tests.EditMode")]
