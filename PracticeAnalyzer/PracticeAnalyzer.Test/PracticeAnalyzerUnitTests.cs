using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = PracticeAnalyzer.Test.CSharpCodeFixVerifier<
    PracticeAnalyzer.PracticeAnalyzerAnalyzer,
    PracticeAnalyzer.PracticeAnalyzerCodeFixProvider>;

namespace PracticeAnalyzer.Test
{
    [TestClass]
    public class PracticeAnalyzerUnitTest
    {
        [TestMethod]
        public async Task LocalIntCouldBeConstant_Diagnostic()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System;

class Program
{
    static void Main()
    {
        [|int i = 0;|]
        Console.WriteLine(i);
    }
}
", @"
using System;

class Program
{
    static void Main()
    {
        const int i = 0;
        Console.WriteLine(i);
    }
}
");
        }       
    }
}
