using System.Runtime.CompilerServices;

namespace WindowsAzure.ServiceBus.Cqs
{
    /// <summary>
    /// <a href="http://martinfowler.com/bliki/CommandQuerySeparation.html">Command/Query separation</a> library.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This library takes the process a bit further than CQS to further decouple your application. We have four different types of message objects.
    /// </para>
    /// <list type="table">
    /// <item>
    /// <term>Command</term>
    /// <description>Just as in the CQS definition commands may not return a value. You should therefore assign any ID's before sending away a command. Commands
    /// are used to modify state in your application. We strongly recommend that a command wrap an entire use case and not just a part of it. Else it will be difficult to scale (now or in the future).
    /// </description>
    /// </item>
    /// <item>
    /// <term>Query</term>
    /// <description>
    /// Queries are used to read data. They should not modify anything. Do not try to make queries generic, make them specific for each use case.
    /// </description>
    /// </item>
    /// <item>
    /// <term>Application event</term>
    /// <description>
    /// Application events are used to notify the entire application of state changes. Like a user have logged in or that a new forum post have been submitted. These events are used
    /// to allow different parts of your system interact in a loosely coupled way. It's even possible that the different parts are in different Azure Cloud Services (but then you have to use Queue Topics).
    /// </description>
    /// </item>
    /// <item>
    /// <term>The last option is Request/Reply</term>
    /// <description>
    /// It's when you absolutely require to get a result back from a modification operation. i.e. like a command with a result. You should prefer to use commands over request/reply as
    /// the former will perform better.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// We strongly recommend that you 
    /// </para>
    /// </remarks>
    [CompilerGenerated]
    internal class NamespaceDoc
    {
    }
}
