using System.Text;
using BGC.Scripting;
using BGC.Scripting.Parsing;

namespace TASagentTwitchBot.ScriptTest;

public class Program
{
    static void Main(string[] args)
    {
        RunTest(InitialScriptTest);
        RunTest(BreakTest);
        RunTest(NaNTest);
        RunTest(WhileTest);
        RunTest(TernaryTest);
        RunTest(ReturnTest);
        RunTest(StringConcatenationTest);
        RunTest(ListTests);
        RunTest(QueueTests);
        RunTest(StackTests);
        RunTest(RingBufferTests);
        RunTest(MathTests);
        RunTest(RandomTests);
        RunTest(InitializerTests);
        RunTest(DepletableTests);
        RunTest(ForEachTests);
        RunTest(ListAssignmentAndNullTests);
        RunTest(ArrayTests);
        RunTest(RecursionTests);
        RunTest(DictionaryRecursionTests);
        RunTest(DictionaryTests);
        RunTest(HashSetTests);
        RunTest(ConstTests);
        RunTest(GlobalDelcarationErrorTest);
        RunTest(ConstantEqualityTests);
        RunTest(ToStringTests);
        RunTest(TestOverloadedOperators);
        RunTest(TestStringInterpolation);
        RunTest(TestGenerics);
        RunTest(TestBinding);
        RunTest(TestCasting);
        RunTest(TestStaticBinding);
        RunTest(TestEnums);
        RunTest(TestValues);
        RunTest(TestFunctionInvocations);
        RunTest(TestReturn);
        RunTest(TestParameterArgumentModifiers);

        Console.ReadKey();
    }

    static void RunTest(Func<List<bool>> testMethod)
    {
        string testName = testMethod.Method.Name;
        string testInfoString = "";

        try
        {
            List<bool> results = testMethod();

            testInfoString = $"{results.Count} Test{(results.Count != 1 ? "s" : "")}";
            bool allTestsPassed = results.All(x => x);

            if (allTestsPassed)
            {
                Console.Write($"    {testName,-30} {testInfoString,-10} ");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("(Passed)");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(" ** ");
                Console.ForegroundColor = ConsoleColor.Gray;

                Console.Write($"{testName,-30} {testInfoString,-10} ");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("(Failed)");
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            for (int i = 0; i < results.Count; i++)
            {
                if (!results[i])
                {
                    Console.WriteLine($"      Failed Test {i + 1}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(" ** ");
            Console.ForegroundColor = ConsoleColor.Gray;

            Console.Write($"{testName,-30} {testInfoString,-10} ");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("(EXCEPTED)");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();
            Console.WriteLine(FormatAndIndentException(ex));
        }
    }

    private static string FormatAndIndentException(Exception ex)
    {
        StringBuilder stringBuilder = new StringBuilder();

        stringBuilder.AppendLine(Indent(4, 4, $"[{ex.GetType().FullName}]"));
        stringBuilder.AppendLine(Indent(4, 6, $"Message: {ex.Message}"));

        if (!string.IsNullOrEmpty(ex.Source))
        {
            stringBuilder.AppendLine(Indent(4, 6, $"Source:  {ex.Source}"));
        }

        if (!string.IsNullOrEmpty(ex.StackTrace))
        {
            stringBuilder.AppendLine(Indent(4, 4, $"Trace:\n{ex.StackTrace}"));
        }

        return stringBuilder.ToString();
    }

    private static string Indent(
        int initialIndent,
        int subsequentIndent,
        string input) => $"{new string(' ', initialIndent)}{input.Replace("\n", $"\n{new string(' ', subsequentIndent)}")}";

    static List<bool> InitialScriptTest()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        globalContext.DeclareVariable("localPreInc", typeof(int), 0);
        globalContext.DeclareVariable("localPostInc", typeof(int), 101);
        globalContext.DeclareVariable("globalPreInc", typeof(int), 1000);

        string testScript = @"
            //These initialization expressions should be skipped
            global int localPreInc = 1000;
            global int localPostInc;
            global int globalPreInc = 200;

            //This one will not be skipped
            global int globalPostInc = 201;

            int argInt;

            void SetupFunction(int argument)
            {
                argInt = argument;

                globalPreInc = 200;
                for (int i = 0; i < 10; i++)
                {
                    localPostInc++;
                    ++globalPreInc;
                    globalPostInc++;
                    ++localPreInc;
                }
            }

            List<bool> RunTests()
            {
                List<bool> tests = new List<bool>();
                tests.Add(localPreInc == 10);
                tests.Add(localPostInc == 111);
                tests.Add(globalPreInc == 210);
                tests.Add(globalPostInc == 211);
                tests.Add(argInt == 666);

                return tests;
            }";


        Script script = ScriptParser.LexAndParseScript(
            script: testScript,
            new FunctionSignature(
                identifier: "SetupFunction",
                returnType: typeof(void),
                arguments: new ArgumentData("argument", typeof(int))),
            new FunctionSignature(
                identifier: "RunTests",
                returnType: typeof(List<bool>)));

        ScriptRuntimeContext context = script.PrepareScript(globalContext);

        script.ExecuteFunction("SetupFunction", context, 666);

        List<bool> tests = script.ExecuteFunction<List<bool>>("RunTests", context);

        tests.Add(globalContext.GetExistingValue<int>("localPreInc") == 10);
        tests.Add(globalContext.GetExistingValue<int>("localPostInc") == 111);
        tests.Add(globalContext.GetExistingValue<int>("globalPreInc") == 210);
        tests.Add(globalContext.GetExistingValue<int>("globalPostInc") == 211);

        return tests;
    }

    static List<bool> BreakTest()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        string testScript = @"
            //Some global variables  (all default initialize to 0)
            global int countLessThan10;
            global int countLessThanEqualTo15;
            global int countLessThan50;
            global int endI;

            //Local Variable
            int testEndI;

            void SetupFunction()
            {
                for (int i = 0; i < 50; i++)
                {
                    if (i < 10)
                        countLessThan10++;

                    if (i <= 15)
                    {
                        countLessThanEqualTo15++;
                    }

                    if (i < 50) countLessThan50++;

                    if (i == 30)
                    {
                        testEndI = i;
                        break;
                    }
                }
                endI = testEndI;
            }

            List<bool> RunTests()
            {
                List<bool> tests = new List<bool>();
                tests.Add(countLessThan10 == 10);
                tests.Add(countLessThanEqualTo15 == 16);
                tests.Add(countLessThan50 == 31);
                tests.Add(endI == 30);

                return tests;
            }";


        Script script = ScriptParser.LexAndParseScript(
            script: testScript,
            new FunctionSignature(
                identifier: "SetupFunction",
                returnType: typeof(void)),
            new FunctionSignature(
                identifier: "RunTests",
                returnType: typeof(List<bool>)));

        ScriptRuntimeContext context = script.PrepareScript(globalContext);

        script.ExecuteFunction("SetupFunction", context);
        return script.ExecuteFunction<List<bool>>("RunTests", context);
    }

    static List<bool> NaNTest()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        string testScript = @"
            //This is a test of some features of Double
            global double testValue = NaN;
            global string testString = ""Original String""; //Another inline comment
            /* A block comment */

            void SetupFunction()
            {
                if (double.IsNaN(testValue))
                {
                    testValue = 3.0*4.0 + 2.0 * 6;
                    testValue *= testValue;

                    testString = ""New String"";
                }
            }

            List<bool> RunTests()
            {
                List<bool> tests = new List<bool>();
                tests.Add(testValue == 576);
                tests.Add(testString == ""New String"");
                return tests;
            }";


        Script script = ScriptParser.LexAndParseScript(testScript,
            new FunctionSignature(
                identifier: "SetupFunction",
                returnType: typeof(void)),
            new FunctionSignature(
                identifier: "RunTests",
                returnType: typeof(List<bool>)));

        ScriptRuntimeContext context = script.PrepareScript(globalContext);

        script.ExecuteFunction("SetupFunction", context);
        List<bool> tests = script.ExecuteFunction<List<bool>>("RunTests", context);

        tests.Add(context.GetExistingValue<double>("testValue") == 576);
        tests.Add(context.GetExistingValue<string>("testString") == "New String");

        return tests;
    }

    static List<bool> WhileTest()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        string testScript = @"
            //This will test some While features
            global int primeFactors = 0;

            // 45750
            int numberToFactorize = 2*3*5*5*5*61;
            global bool matchesExpectation = numberToFactorize == 45750;

            int factor = 2;

            void SetupFunction()
            {
                while (numberToFactorize > 1)
                {
                    while (numberToFactorize % factor == 0)
                    {
                        primeFactors++;
                        numberToFactorize /= factor;
                    }

                    if (factor == 2)
                    {
                        factor += 1;
                    }
                    else
                    {
                        factor += 2;
                    }
                }
            }

            List<bool> RunTests()
            {
                List<bool> tests = new List<bool>();
                tests.Add(matchesExpectation == true);
                tests.Add(primeFactors == 6);
                return tests;
            }";


        Script script = ScriptParser.LexAndParseScript(testScript,
            new FunctionSignature(
                identifier: "SetupFunction",
                returnType: typeof(void)),
            new FunctionSignature(
                identifier: "RunTests",
                returnType: typeof(List<bool>)));

        ScriptRuntimeContext context = script.PrepareScript(globalContext);

        script.ExecuteFunction("SetupFunction", context);
        return script.ExecuteFunction<List<bool>>("RunTests", context);
    }

    static List<bool> TernaryTest()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        string testScript = @"
            //This will test the ternary operator
            global double assignee1 = 0;
            global double assignee2 = 0;
            global double assignee3 = 0;

            bool test = false;

            void SetupFunction()
            {
                assignee1 = !test ? 1 : 2;
                assignee2 = test ? 1.0 : 2;
                assignee3 = test ? 1.0 : 2.0;
            }

            List<bool> RunTests()
            {
                List<bool> tests = new List<bool>();
                tests.Add(assignee1 == 1);
                tests.Add(assignee2 == 2);
                tests.Add(assignee3 == 2);
                return tests;
            }";

        Script script = ScriptParser.LexAndParseScript(testScript,
            new FunctionSignature(
                identifier: "SetupFunction",
                returnType: typeof(void)),
            new FunctionSignature(
                identifier: "RunTests",
                returnType: typeof(List<bool>)));

        ScriptRuntimeContext context = script.PrepareScript(globalContext);

        script.ExecuteFunction("SetupFunction", context);
        return script.ExecuteFunction<List<bool>>("RunTests", context);
    }

    static List<bool> ReturnTest()
    {
        List<bool> tests = new List<bool>();

        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        string testScriptA = @"
            double tempA = 0.0;

            bool TestFunction()
            {
                if (tempA < 1.0)
                    return true;
                else
                    return false;
            }";


        Script script = ScriptParser.LexAndParseScript(testScriptA,
            new FunctionSignature(
                identifier: "TestFunction",
                returnType: typeof(bool)));

        ScriptRuntimeContext context = script.PrepareScript(globalContext);

        tests.Add(script.ExecuteFunction<bool>("TestFunction", context));

        string testScriptB = @"
            double tempA = 0.0;

            bool TestFunction()
            {
                if (tempA < 1.0)
                    return true;
                else
                    return;
            }";

        bool excepted = false;
        bool correctException = false;
        try
        {
            //This should except out
            ScriptParser.LexAndParseScript(testScriptB, new FunctionSignature("TestFunction", typeof(bool)));
        }
        catch (Exception ex)
        {
            excepted = true;
            if (ex is ScriptParsingException)
            {
                correctException = true;
            }
        }

        tests.Add(excepted);
        tests.Add(correctException);

        return tests;
    }


    static List<bool> StringConcatenationTest()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        string testScript = @"
            string testString1 = ""Oh Hai"";
            string testString2 = ""!"";
            global string testString3 = testString1 + testString2 + 3;
            global string testString4;
            global int testLength1; 
            global int testLength3; 
            global int testLength4;

            void SetupFunction()
            {
                testString4 += ""Test "";
                testString4 += 4;

                testLength1 = ""Oh Hai"".Length;
                testLength3 = testString3.Length;
                testLength4 = testString4.Length;
            }

            List<bool> RunTests()
            {
                List<bool> tests = new List<bool>();
                tests.Add(testString3 == ""Oh Hai!3"");
                tests.Add(testString4 == ""Test 4"");
                tests.Add(testLength1 == 6);
                tests.Add(testLength3 == 8);
                tests.Add(testLength4 == 6);
                return tests;
            }";


        Script script = ScriptParser.LexAndParseScript(testScript,
            new FunctionSignature(
                identifier: "SetupFunction",
                returnType: typeof(void)),
            new FunctionSignature(
                identifier: "RunTests",
                returnType: typeof(List<bool>)));

        ScriptRuntimeContext context = script.PrepareScript(globalContext);

        script.ExecuteFunction("SetupFunction", context);
        return script.ExecuteFunction<List<bool>>("RunTests", context);
    }

    static List<bool> ListTests()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        globalContext.DeclareVariable("boolList", typeof(List<bool>), new List<bool>() { true, false, true });

        string testScript = @"
            global List<int> intList = new List<int>();
            global List<double> doubleList = new List<double>();
            global List<string> stringList = new List<string>();

            extern List<bool> boolList;

            global bool testBool = boolList[2];
            global bool intListTest1;
            global bool intListTest2;
            global int intListTest3;

            void SetupFunction()
            {
                boolList[1] = true;

                intList.Add(5);

                intListTest1 = intList.Contains(5);
                intListTest2 = intList.Contains(6);

                intList.Add(6);
                intListTest3 = intList.IndexOf(6);
            }

            List<bool> RunTests()
            {
                List<bool> tests = new List<bool>();
                tests.Add(intList != null);
                tests.Add(doubleList != null);
                tests.Add(stringList != null);
                tests.Add(boolList != null);
                tests.Add(boolList[1] == true);
                tests.Add(testBool == true);

                tests.Add(intList.Count == 2);
                tests.Add(intList[0] == 5);
                tests.Add(intListTest1 == true);
                tests.Add(intListTest2 == false);
                tests.Add(intListTest3 == 1);

                return tests;
            }";

        Script script = ScriptParser.LexAndParseScript(testScript,
            new FunctionSignature(
                identifier: "SetupFunction",
                returnType: typeof(void)),
            new FunctionSignature(
                identifier: "RunTests",
                returnType: typeof(List<bool>)));

        ScriptRuntimeContext context = script.PrepareScript(globalContext);

        script.ExecuteFunction("SetupFunction", context);
        return script.ExecuteFunction<List<bool>>("RunTests", context);
    }

    static List<bool> QueueTests()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        string testScript = @"
            Queue<int> intQueue = new Queue<int>();

            List<bool> RunTests()
            {
                List<bool> tests = new List<bool>();

                for (int i = 0; i < 12; i++)
                {
                    intQueue.Enqueue(i);
                }

                tests.Add(intQueue.Dequeue() == 0 && intQueue.Dequeue() == 1);
                tests.Add(intQueue.Contains(4));
                tests.Add(!intQueue.Contains(1));
                tests.Add(intQueue.Peek() == 2);
                tests.Add(intQueue.Count == 10);
                intQueue.Enqueue(100);
                tests.Add(intQueue.Count == 11);

                while (intQueue.Count > 1)
                {
                    intQueue.Dequeue();
                }

                tests.Add(intQueue.Dequeue() == 100);

                return tests;
            }";


        Script script = ScriptParser.LexAndParseScript(testScript,
            new FunctionSignature(
                identifier: "RunTests",
                returnType: typeof(List<bool>)));

        ScriptRuntimeContext context = script.PrepareScript(globalContext);
        return script.ExecuteFunction<List<bool>>("RunTests", context);
    }

    static List<bool> StackTests()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        string testScript = @"
            Stack<int> intStack = new Stack<int>();

            List<bool> RunTests()
            {
                List<bool> tests = new List<bool>();

                for (int i = 0; i < 12; i++)
                {
                    intStack.Push(i);
                }

                tests.Add(intStack.Pop() == 11 && intStack.Pop() == 10);
                tests.Add(intStack.Contains(8));
                tests.Add(!intStack.Contains(10));
                tests.Add(intStack.Peek() == 9);
                tests.Add(intStack.Count == 10);
                intStack.Push(100);
                tests.Add(intStack.Count == 11);

                while (intStack.Count > 1)
                {
                    intStack.Pop();
                }

                tests.Add(intStack.Pop() == 0);

                return tests;
            }";

        Script script = ScriptParser.LexAndParseScript(testScript,
            new FunctionSignature(
                identifier: "RunTests",
                returnType: typeof(List<bool>)));

        ScriptRuntimeContext context = script.PrepareScript(globalContext);
        return script.ExecuteFunction<List<bool>>("RunTests", context);
    }

    static List<bool> RingBufferTests()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        string testScript = @"
            RingBuffer<int> intBuffer = new RingBuffer<int>(10);

            List<bool> RunTests()
            {
                List<bool> tests = new List<bool>();

                intBuffer.Push(0);
                intBuffer.Push(0);
                intBuffer.Push(0);

                tests.Add(intBuffer.Count == 3);

                for (int i = 0; i < 12; i++)
                {
                    intBuffer.Push(i);
                }

                tests.Add(intBuffer.Count == 10);
                tests.Add(intBuffer.Head == 11);
                tests.Add(intBuffer.Tail == 2);

                intBuffer.Push(12);
                tests.Add(intBuffer.Head == 12);
                tests.Add(intBuffer.Tail == 3);

                tests.Add(intBuffer.PopBack() == 3);
                tests.Add(intBuffer.Count == 9);


                tests.Add(intBuffer.PeekHead() == 12);
                tests.Add(intBuffer.PeekTail() == 4);
                tests.Add(intBuffer[3] == 9);
                intBuffer.RemoveAt(3);
                tests.Add(intBuffer[3] == 8);
                intBuffer.Add(99);
                tests.Add(intBuffer.Head == 99);

                tests.Add(intBuffer.Contains(99));
                tests.Add(intBuffer.Remove(99));
                tests.Add(intBuffer.Contains(99) == false);

                int index = intBuffer.GetIndex(6);
                tests.Add(index != -1);
                intBuffer.RemoveAt(index);
                tests.Add(intBuffer.GetIndex(6) == -1);


                intBuffer.Clear();
                tests.Add(intBuffer.Count == 0);
                tests.Add(intBuffer.Size == 10);
        
                return tests;
            }";

        Script script = ScriptParser.LexAndParseScript(testScript,
            new FunctionSignature(
                identifier: "RunTests",
                returnType: typeof(List<bool>)));

        ScriptRuntimeContext context = script.PrepareScript(globalContext);
        return script.ExecuteFunction<List<bool>>("RunTests", context);
    }

    static List<bool> MathTests()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        string testScript = @"
            List<bool> RunTests()
            {
                List<bool> tests = new List<bool>();

        
                tests.Add(Math.Floor(10.1) == 10);
                tests.Add(Math.Floor(10.0) == 10);
                tests.Add(Math.Floor(9.9999) == 9);

                tests.Add(Math.Ceiling(10.1) == 11);
                tests.Add(Math.Ceiling(10.0) == 10);
                tests.Add(Math.Ceiling(9.9999) == 10);

                tests.Add(Math.Round(10.1) == 10);
                tests.Add(Math.Round(10.0) == 10);
                tests.Add(Math.Round(9.9999) == 10);

                tests.Add(Math.Log(Math.E) == 1);

                return tests;
            }";

        Script script = ScriptParser.LexAndParseScript(testScript,
            new FunctionSignature(
                identifier: "RunTests",
                returnType: typeof(List<bool>)));

        ScriptRuntimeContext context = script.PrepareScript(globalContext);
        return script.ExecuteFunction<List<bool>>("RunTests", context);
    }

    static List<bool> RandomTests()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        string testScript = @"
            List<bool> RunTests()
            {
                List<bool> tests = new List<bool>();

                Random random = new Random();
                int randomSeed = random.Next();

                Random randomA = new Random(randomSeed);
                Random randomB = new Random(randomSeed);
        
                tests.Add(randomA.Next() == randomB.Next());
                tests.Add(randomA.Next() == randomB.Next());
                tests.Add(randomA.Next() == randomB.Next());
                tests.Add(randomA.Next() == randomB.Next());

                tests.Add(randomA.NextDouble() == randomB.NextDouble());
                tests.Add(randomA.NextDouble() == randomB.NextDouble());
                tests.Add(randomA.NextDouble() == randomB.NextDouble());
                tests.Add(randomA.NextDouble() == randomB.NextDouble());

                int lowerBound = random.Next(10);
                int upperBound = random.Next(lowerBound + 10, lowerBound + 20);

                tests.Add(randomA.Next(lowerBound, upperBound) == randomB.Next(lowerBound, upperBound));
                tests.Add(randomA.Next(lowerBound, upperBound) == randomB.Next(lowerBound, upperBound));
                tests.Add(randomA.Next(lowerBound, upperBound) == randomB.Next(lowerBound, upperBound));
                tests.Add(randomA.Next(lowerBound, upperBound) == randomB.Next(lowerBound, upperBound));

                return tests;
            }";

        Script script = ScriptParser.LexAndParseScript(testScript,
            new FunctionSignature(
                identifier: "RunTests",
                returnType: typeof(List<bool>)));

        ScriptRuntimeContext context = script.PrepareScript(globalContext);
        return script.ExecuteFunction<List<bool>>("RunTests", context);
    }

    static List<bool> InitializerTests()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        string testScript = @"
            bool exampleBoolA = true;
            bool exampleBoolB = false;
        
            List<bool> RunTests()
            {
                List<bool> tests = new List<bool>() { true, exampleBoolA, !exampleBoolB };

                List<int> listTests = new List<int>() {1, 2, 3, 4, 5};

                tests.Add(listTests.Count == 5);
                tests.Add(listTests[2] == 3);

                Queue<double> queueTests = new Queue<double>() { 1, 2.0, 3.5 };
                tests.Add(queueTests.Count == 3);
                tests.Add(queueTests.Dequeue() == 1.0);
                tests.Add(queueTests.Dequeue() == 2.0);
                tests.Add(queueTests.Dequeue() == 3.5);

                Stack<double> stackTests = new Stack<double>() { 3.5, 2.0, 1 };
                tests.Add(stackTests.Count == 3);
                tests.Add(stackTests.Pop() == 1);
                tests.Add(stackTests.Pop() == 2.0);
                tests.Add(stackTests.Pop() == 3.5);

                RingBuffer<double> ringBufferTests = new RingBuffer<double>(5) { 1, 2, 3 };
                tests.Add(ringBufferTests.Count == 3);
                tests.Add(ringBufferTests.Pop() == 3);
                tests.Add(ringBufferTests.PopBack() == 1);
                tests.Add(ringBufferTests.PeekHead() == 2);
                tests.Add(ringBufferTests.PeekTail() == 2);

                DepletableBag<double> depletableBagTests = new DepletableBag<double>() { 1, 2, 3 };
                tests.Add(depletableBagTests.Count == 3);

                DepletableList<string> depletableListTests = new DepletableList<string>() {
                    ""first"", ""second"", ""third""
                };
                tests.Add(depletableListTests.Count == 3);
                tests.Add(depletableListTests.PopNext() == ""first"");

                return tests;
            }";

        Script script = ScriptParser.LexAndParseScript(testScript,
            new FunctionSignature(
                identifier: "RunTests",
                returnType: typeof(List<bool>)));

        ScriptRuntimeContext context = script.PrepareScript(globalContext);
        return script.ExecuteFunction<List<bool>>("RunTests", context);
    }

    static List<bool> DepletableTests()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        string testScript = @"
            List<bool> RunTests()
            {
                List<bool> tests = new List<bool>();

                DepletableBag<double> depletableBagTests = new DepletableBag<double>();
                depletableBagTests.Add(1);
                depletableBagTests.Add(2);
                depletableBagTests.Add(3);
                tests.Add(depletableBagTests.Count == 3);

                double nextValue = depletableBagTests.PopNext();
                tests.Add(nextValue == 1 || nextValue == 2 || nextValue == 3);

                DepletableList<string> depletableListTests = new DepletableList<string>() {
                    ""first"", ""second"", ""third""
                };
                tests.Add(depletableListTests.Count == 3);
                tests.Add(depletableListTests.PopNext() == ""first"");

                Random randomTest1 = new Random(100);
                Random randomTest2 = new Random(100);

                DepletableBag<int> depletableBag1 = new DepletableBag<int>(false, randomTest1) {1, 2, 3, 4, 5, 6, 7, 8, 9, 10};
                DepletableBag<int> depletableBag2 = new DepletableBag<int>(false, randomTest2) {1, 2, 3, 4, 5, 6, 7, 8, 9, 10};

                while (depletableBag1.Count > 0)
                {
                    tests.Add(depletableBag1.PopNext() == depletableBag2.PopNext());
                }

                return tests;
            }";

        Script script = ScriptParser.LexAndParseScript(testScript,
            new FunctionSignature(
                identifier: "RunTests",
                returnType: typeof(List<bool>)));

        ScriptRuntimeContext context = script.PrepareScript(globalContext);
        return script.ExecuteFunction<List<bool>>("RunTests", context);
    }

    static List<bool> ForEachTests()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        string testScript = @"
            List<bool> RunTests()
            {
                List<bool> tests = new List<bool>();
                List<int> items = new List<int>() {1, 2, 3, 4, 5};

                for (int i = 6; i <= 10; i++)
                {
                    items.Add(i);
                }

                int index = 0;
                foreach(int item in items)
                {
                    tests.Add(item == ++index);
                }

                index = 0;
                foreach(int item in new List<int>() {1, 2, 3, 4, 5})
                {
                    tests.Add(item == ++index);
                }
                return tests;
            }";

        Script script = ScriptParser.LexAndParseScript(testScript,
            new FunctionSignature(
                identifier: "RunTests",
                returnType: typeof(List<bool>)));

        ScriptRuntimeContext context = script.PrepareScript(globalContext);
        return script.ExecuteFunction<List<bool>>("RunTests", context);
    }

    static List<bool> ListAssignmentAndNullTests()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        string testScript = @"
            List<bool> RunTests()
            {
                List<bool> tests = new List<bool>();
                List<int> items = new List<int>() {1, 2, 3, 4, 5};
                List<int> newItems = null;
                List<int> newItems2;
                List<int> newItems3 = new List<int>(items);

                tests.Add(newItems == newItems2);

                tests.Add(newItems == null);
                tests.Add(newItems != items);

                newItems = items;

                tests.Add(newItems != null);
                tests.Add(newItems == items);
                tests.Add(newItems[0] == 1);
                tests.Add(newItems[4] == 5);
        
                items.Clear();
                tests.Add(newItems.Count == 0);

                tests.Add(newItems3[0] == 1);
                tests.Add(newItems3[4] == 5);

                return tests;
            }";

        Script script = ScriptParser.LexAndParseScript(testScript,
            new FunctionSignature(
                identifier: "RunTests",
                returnType: typeof(List<bool>)));

        ScriptRuntimeContext context = script.PrepareScript(globalContext);
        return script.ExecuteFunction<List<bool>>("RunTests", context);
    }


    static List<bool> ArrayTests()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        string testScript = @"
            List<bool> RunTests()
            {
                List<bool> tests = new List<bool>();

                int[] items = new int[5];
                items[0] = 1;
                items[1] = 2;
                items[2] = 5;
                items[2] = 3;
                items[3] = 4;
                items[4] = 5;

                int[] moreItems = new int[] { 1, 5, 3, 4, 5 };
                moreItems[1] = 2;

                tests.Add(items[0] == 1);
                tests.Add(items[1] == 2);
                tests.Add(items[2] == 3);
                tests.Add(items[3] == 4);
                tests.Add(items[4] == 5);

                tests.Add(items[0] == moreItems[0]);
                tests.Add(items[1] == moreItems[1]);
                tests.Add(items[2] == moreItems[2]);
                tests.Add(items[3] == moreItems[3]);
                tests.Add(items[4] == moreItems[4]);

                int[] moreMoreItems = new int[5] { 1, 2, 3, 4, 5 };

                return tests;
            }";

        Script script = ScriptParser.LexAndParseScript(testScript,
            new FunctionSignature(
                identifier: "RunTests",
                returnType: typeof(List<bool>)));

        ScriptRuntimeContext context = script.PrepareScript(globalContext);
        return script.ExecuteFunction<List<bool>>("RunTests", context);
    }

    static List<bool> RecursionTests()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        string testScript = @"
            int testValue = 0;

            List<bool> RunTests()
            {
                List<bool> tests = new List<bool>();

                tests.Add(FibonacciNumber(-1) == 0);
                tests.Add(FibonacciNumber(0) == 1);
                tests.Add(FibonacciNumber(1) == 1);
                tests.Add(FibonacciNumber(2) == 2);
                tests.Add(FibonacciNumber(3) == 3);
                tests.Add(FibonacciNumber(4) == 5);
                tests.Add(FibonacciNumber(5) == 8);
                tests.Add(FibonacciNumber(6) == 13);


                tests.Add(testValue == 0);

                IncrementBy(10);
                tests.Add(testValue == 10);
                IncrementBy(10);
                tests.Add(testValue == 20);
                IncrementBy(FibonacciNumber(6));
                tests.Add(testValue == 33);
                IncrementBy(FibonacciNumber(FibonacciNumber(4)));
                tests.Add(testValue == 41);

                return tests;
            }

            int FibonacciNumber(int index)
            {
                if (index < 0)
                    return 0;
                if (index < 2)
                    return 1;

                return FibonacciNumber(index - 1) + FibonacciNumber(index - 2);
            }
            
            void IncrementBy(int value)
            {
                testValue += value;
            }";

        Script script = ScriptParser.LexAndParseScript(
            script: testScript,
            new FunctionSignature(
                identifier: "RunTests",
                returnType: typeof(List<bool>)),
            new FunctionSignature(
                identifier: "FibonacciNumber",
                returnType: typeof(int),
                arguments: new ArgumentData("index", typeof(int))));

        ScriptRuntimeContext context = script.PrepareScript(globalContext);
        return script.ExecuteFunction<List<bool>>("RunTests", context);
    }

    static List<bool> DictionaryRecursionTests()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        string testScript = @"
            int testValue = 0;
            Dictionary<int,int> cachedValues = new Dictionary<int,int>();

            List<bool> RunTests()
            {
                List<bool> tests = new List<bool>();

                tests.Add(FibonacciNumber(-1) == 0);
                tests.Add(FibonacciNumber(0) == 1);
                tests.Add(FibonacciNumber(1) == 1);
                tests.Add(FibonacciNumber(2) == 2);
                tests.Add(FibonacciNumber(3) == 3);
                tests.Add(FibonacciNumber(4) == 5);
                tests.Add(FibonacciNumber(5) == 8);
                tests.Add(FibonacciNumber(6) == 13);


                tests.Add(testValue == 0);

                IncrementBy(10);
                tests.Add(testValue == 10);
                IncrementBy(10);
                tests.Add(testValue == 20);
                IncrementBy(FibonacciNumber(6));
                tests.Add(testValue == 33);
                IncrementBy(FibonacciNumber(FibonacciNumber(4)));
                tests.Add(testValue == 41);

                return tests;
            }

            int FibonacciNumber(int index)
            {
                if (index < 0) return 0;
                if (index < 2) return 1;

                if (!cachedValues.ContainsKey(index))
                {
                    cachedValues.Add(index, FibonacciNumber(index - 1) + FibonacciNumber(index - 2));
                }

                return cachedValues[index];
            }
            
            void IncrementBy(int value)
            {
                testValue += value;
            }";

        Script script = ScriptParser.LexAndParseScript(
            script: testScript,
            new FunctionSignature(
                identifier: "RunTests",
                returnType: typeof(List<bool>)),
            new FunctionSignature(
                identifier: "FibonacciNumber",
                returnType: typeof(int),
                arguments: new ArgumentData("index", typeof(int))));

        ScriptRuntimeContext context = script.PrepareScript(globalContext);
        List<bool> tests = script.ExecuteFunction<List<bool>>("RunTests", context);
        tests.Add(script.ExecuteFunction<int>("FibonacciNumber", context, 25) == 121393);

        return tests;
    }

    static List<bool> DictionaryTests()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        string testScript = @"
            int testValue = 0;
            Dictionary<string,double> map = new Dictionary<string,double>();

            List<bool> RunTests()
            {
                List<bool> tests = new List<bool>();

                map.Add(""A"",0);
                map.Add(""B"",1);
                map.Add(""C"",2.1);
                map.Add(""D"",3);
                map.Add(""E"",4);
                map.Add(""F"",5);
                map.Add(""G"",6);
                map.Add(""H"",7);
                map.Add(""TASagent"",99);


                tests.Add(map.Count == 9);
                tests.Add(map.ContainsKey(""TASagent""));
                tests.Add(!map.ContainsKey(""I""));
                tests.Add(map[""B""] == 1);
                tests.Add(map[""TASagent""] == 99);
                tests.Add(map.Remove(""TASagent""));
                tests.Add(!map.Remove(""TASagent""));
                tests.Add(!map.ContainsKey(""TASagent""));
                tests.Add(map.ContainsValue(2.1));
                tests.Add(!map.ContainsValue(2));

                Queue<string> keys = new Queue<string>(map.Keys);
                Queue<double> values = new Queue<double>(map.Values);

                foreach(string key in map.Keys)
                {
                    tests.Add(keys.Dequeue() == key);
                }

                foreach(double value in map.Values)
                {
                    tests.Add(values.Dequeue() == value);
                }

                tests.Add(keys.Count == 0);
                tests.Add(values.Count == 0);

                return tests;
            }";

        Script script = ScriptParser.LexAndParseScript(
            script: testScript,
            new FunctionSignature(
                identifier: "RunTests",
                returnType: typeof(List<bool>)));

        ScriptRuntimeContext context = script.PrepareScript(globalContext);
        return script.ExecuteFunction<List<bool>>("RunTests", context);
    }

    static List<bool> HashSetTests()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        string testScript = @"
            List<bool> RunTests()
            {
                List<bool> tests = new List<bool>();
                List<int> startingFactors = new List<int>() {2, 2, 2, 2, 3, 3, 3, 17};
                int number = 1;
                List<int> calculatedFactors = new List<int>();
                HashSet<int> testedNumbers = new HashSet<int>();

                //Multiply factors together
                foreach(int factor in startingFactors)
                {
                    number *= factor;
                }

                int nextFactor;
                while (number > 1)
                {
                    nextFactor = Factorize(number, testedNumbers);
                    if (nextFactor > 0)
                    {
                        calculatedFactors.Add(nextFactor);
                        number /= nextFactor;
                    }
                }

                tests.Add(calculatedFactors.Count == startingFactors.Count);

                for (int i = 0; i < calculatedFactors.Count; i++)
                {
                    tests.Add(calculatedFactors[i] == startingFactors[i]);
                }

                return tests;
            }


            int Factorize(int number, HashSet<int> testedNumbers)
            {
                int factorToTest = 2;
                while (testedNumbers.Contains(factorToTest))
                {
                    factorToTest++;
                }

                if (number % factorToTest == 0)
                    return factorToTest;

                testedNumbers.Add(factorToTest);
                return 0;
            }";

        Script script = ScriptParser.LexAndParseScript(
            script: testScript,
            new FunctionSignature(
                identifier: "RunTests",
                returnType: typeof(List<bool>)));

        ScriptRuntimeContext context = script.PrepareScript(globalContext);
        return script.ExecuteFunction<List<bool>>("RunTests", context);
    }

    static List<bool> ConstTests()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        string testScript = @"
            const int testInt = 100;
            const int otherTestInt = testInt + 100;
            const double testDouble = 100.0;

            List<bool> RunTests()
            {
                List<bool> tests = new List<bool>();
            
                tests.Add(testInt == 100);
                tests.Add(otherTestInt == 200);
                tests.Add(testDouble == 100.0);

                if (testInt == 100)
                {
                    tests.Add(true);
                }
                else
                    tests.Add(false);

                const int otherTest = 20;

                tests.Add(otherTest == 20);

                tests.Add(5 * otherTest == testInt);

                return tests;
            }";

        Script script = ScriptParser.LexAndParseScript(
            script: testScript,
            new FunctionSignature(
                identifier: "RunTests",
                returnType: typeof(List<bool>)));

        ScriptRuntimeContext context = script.PrepareScript(globalContext);
        return script.ExecuteFunction<List<bool>>("RunTests", context);
    }

    static List<bool> GlobalDelcarationErrorTest()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        string testScript = @"
            //Initialized with integer literal
            global double testDouble = 25;
            const double testConstDouble = 50;
            double localDouble = 75;

            void RunTest()
            {
                double testA = testDouble + 1.0;
                double testB = testDouble + 1;
                double testC = testConstDouble + 1;
                double testD = testConstDouble + 1.0;
                double testE = localDouble + 1;
                double testF = localDouble + 1.0;
            }";

        Script script = ScriptParser.LexAndParseScript(
            script: testScript,
            new FunctionSignature(
                identifier: "RunTest",
                returnType: typeof(void)));

        ScriptRuntimeContext context = script.PrepareScript(globalContext);
        script.ExecuteFunction("RunTest", context);

        return new List<bool>() { true };
    }

    static List<bool> ConstantEqualityTests()
    {
        List<bool> tests = new List<bool>();
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        string testScript = @"
            int RunTests()
            {
                if ( 1.0 != 1 )
                {
                    return 1;
                }

                if ( 1.0 == 1 )
                {
                    //Continue
                }
                else
                {
                    return 2;
                }

                return 0;
            }";

        Script script = ScriptParser.LexAndParseScript(
            script: testScript,
            new FunctionSignature(
                identifier: "RunTests",
                returnType: typeof(int)));

        ScriptRuntimeContext context = script.PrepareScript(globalContext);

        tests.Add(script.ExecuteFunction<int>("RunTests", context) == 0);

        return tests;
    }


    static List<bool> ToStringTests()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        string testScript = @"
            double testValue = 1054.32179;

            List<bool> RunTests()
            {
                List<bool> tests = new List<bool>();
                tests.Add(testValue.ToString(""E"") == ""1.054322E+003"");
                tests.Add(testValue.ToString(""E0"") == ""1E+003"");
                tests.Add(testValue.ToString(""E1"") == ""1.1E+003"");

                tests.Add(testValue.ToString(""e"") == ""1.054322e+003"");
                tests.Add(testValue.ToString(""e0"") == ""1e+003"");
                tests.Add(testValue.ToString(""e1"") == ""1.1e+003"");

                tests.Add((1054.32179).ToString(""F"") == ""1054.32"");
                tests.Add(testValue.ToString(""F0"") == ""1054"");
                tests.Add(testValue.ToString(""F1"") == ""1054.3"");

                tests.Add(testValue.ToString(""N"") == ""1,054.32"");
                tests.Add(testValue.ToString(""N0"") == ""1,054"");
                tests.Add(testValue.ToString(""N1"") == ""1,054.3"");

                return tests;
            }";


        Script script = ScriptParser.LexAndParseScript(testScript,
            new FunctionSignature(
                identifier: "RunTests",
                returnType: typeof(List<bool>)));

        ScriptRuntimeContext context = script.PrepareScript(globalContext);

        return script.ExecuteFunction<List<bool>>("RunTests", context);
    }

    static List<bool> TestOverloadedOperators()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        string testScript = @"
            List<bool> RunTests()
            {
                List<bool> tests = new List<bool>();

                DateTime now = DateTime.Now;
                DateTime soon = now + new TimeSpan(1, 0, 0);

                tests.Add(DateTime.Now >= now);
                tests.Add(soon > now);

                return tests;
            }";

        Script script = ScriptParser.LexAndParseScript(
            script: testScript,
            new FunctionSignature(
                identifier: "RunTests",
                returnType: typeof(List<bool>)));

        ScriptRuntimeContext context = script.PrepareScript(globalContext);
        return script.ExecuteFunction<List<bool>>("RunTests", context);
    }

    static List<bool> TestStringInterpolation()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        const string TestScriptA = @"
            List<bool> TestStringInterpolation()
            {
                List<bool> testResults = new List<bool>();

                testResults.Add(""ABC"" == $""A{""B""}C"");

                bool temp = true;

                testResults.Add(""ABC"" == $""A{ (temp ? ""B"" : ""C"" )}C"");
                testResults.Add(""ABC"" == $""A{ (!temp ? ""C"" : ""B"" )}C"");
                testResults.Add(""Nested String"" == $""Nes{ $""te{""d S""}t"" }ring"");
                testResults.Add(""Test String"" == $""{""Test""} String"");

                testResults.Add(""ABCDE"" == $""{""A""}{""B""}{""C""}{""D""}{""E""}"");

                return testResults;
            }";

        Script newScript = ScriptParser.LexAndParseScript(
            script: TestScriptA,
            new FunctionSignature("TestStringInterpolation", typeof(List<bool>)));

        ScriptRuntimeContext scriptContext = newScript.PrepareScript(globalContext);

        return newScript.ExecuteFunction<List<bool>>("TestStringInterpolation", 2_000, scriptContext, Array.Empty<object>());
    }

    static List<bool> TestGenerics()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        const string TestGenericsScript = @"
            List<bool> TestGenerics()
            {
                List<bool> tests = new List<bool>();

                List<int> intList = new List<int>() { 0, 1, 2};
                tests.Add(intList[0] == 0);
                tests.Add(intList[1] == 1);
                tests.Add(intList[2] == 2);

                TestGenericClass<int> testClass = new TestGenericClass<int>(5);
                tests.Add(testClass.Value == 5);
                tests.Add(testClass.GetValue() == 5);
                testClass.SetValue(6);
                tests.Add(testClass.GetValue() == 6);
                tests.Add(testClass.Value == 6);
                tests.Add(testClass.TryThing<double>(1.0) == 1.0);
                tests.Add(testClass.TryThing<string>(""hello"") == ""hello"");

                return tests;
            }";

        ClassRegistrar.TryRegisterClass(typeof(TestGenericClass<>));

        Script newScript = ScriptParser.LexAndParseScript(
            script: TestGenericsScript,
            new FunctionSignature("TestGenerics", typeof(List<bool>)));

        ScriptRuntimeContext scriptContext = newScript.PrepareScript(globalContext);

        return newScript.ExecuteFunction<List<bool>>("TestGenerics", 2_000, scriptContext, Array.Empty<object>());
    }

    static List<bool> TestBinding()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        ClassRegistrar.TryRegisterClass<TestMethodOverloadingClass>();


        const string TestBindingScript = @"
            List<bool> TestBinding()
            {
                TestMethodOverloadingClass testObject = new TestMethodOverloadingClass();
                List<bool> tests = new List<bool>();

                tests.Add(testObject.DoThing() == 1);
                tests.Add(testObject.DoThing(1) == 2);
                tests.Add(testObject.DoThing<int>(1, 2) == 3);

                return tests;
            }";

        Script newScript = ScriptParser.LexAndParseScript(
            script: TestBindingScript,
            new FunctionSignature("TestBinding", typeof(List<bool>)));

        ScriptRuntimeContext scriptContext = newScript.PrepareScript(globalContext);

        return newScript.ExecuteFunction<List<bool>>("TestBinding", 2_000, scriptContext, Array.Empty<object>());
    }

    static List<bool> TestStaticBinding()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        ClassRegistrar.TryRegisterClass<TestStaticBindingClass>();

        const string TestStaticBindingScript = @"
            List<bool> TestStaticBinding()
            {
                TestStaticBindingClass testObject = new TestStaticBindingClass();
                List<bool> tests = new List<bool>();

                //Static property access
                tests.Add(TestStaticBindingClass.StaticValue == 0);
                tests.Add(0 == TestStaticBindingClass.StaticValue);

                //Static property operation
                TestStaticBindingClass.StaticValue++;

                tests.Add(TestStaticBindingClass.StaticValue == 1);

                //Static method call Statement
                TestStaticBindingClass.IncrementStaticValue();

                tests.Add(TestStaticBindingClass.StaticValue == 2);

                //Static method call Expression
                tests.Add(TestStaticBindingClass.CheckStaticValue(2));

                tests.Add(testObject.InstanceValue == 0);
                tests.Add(0 == testObject.InstanceValue);


                //Nested calls and access
                tests.Add(TestStaticBindingClass.List.Count == 1);
                TestStaticBindingClass.List.Clear();
                tests.Add(TestStaticBindingClass.List.Count == 0);
                TestStaticBindingClass.List.Add(4);
                tests.Add(TestStaticBindingClass.List[0] == 4);
                

                testObject.InstanceValue++;

                tests.Add(testObject.InstanceValue == 1);

                testObject.IncrementInstanceValue();

                tests.Add(testObject.InstanceValue == 2);
                tests.Add(testObject.CheckInstanceValue(2));

                return tests;
            }";

        Script newScript = ScriptParser.LexAndParseScript(
            script: TestStaticBindingScript,
            new FunctionSignature("TestStaticBinding", typeof(List<bool>)));

        ScriptRuntimeContext scriptContext = newScript.PrepareScript(globalContext);

        return newScript.ExecuteFunction<List<bool>>("TestStaticBinding", 2_000, scriptContext, Array.Empty<object>());
    }

    static List<bool> TestCasting()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        ClassRegistrar.TryRegisterClass<ITestInterface>();
        ClassRegistrar.TryRegisterClass<TestInterface1>();
        ClassRegistrar.TryRegisterClass<TestInterface2>();
        ClassRegistrar.TryRegisterClass<TestInterfaceUnrelatedClass>();

        const string TestCastingScript = @"
            List<bool> TestCasting()
            {
                List<bool> tests = new List<bool>();

                double doubleValue = 2.5;
                int intValue = (int)2.5;

                tests.Add(doubleValue != intValue);
                tests.Add(intValue == 2);
                tests.Add((int)2.5 == 2);

                TestInterface1 testInterface1 = new TestInterface1();
                TestInterface2 testInterface2 = new TestInterface2();
                ITestInterface testInterface = testInterface1;
                testInterface1 = (TestInterface1)testInterface;

                tests.Add(testInterface1.GetTestValue() == 1);
                tests.Add(testInterface.GetTestValue() == 1);

                tests.Add(((ITestInterface)testInterface2).GetTestValue() == 2);

                //In C# this should fail. Should we consider making that happen?
                tests.Add(testInterface2.GetTestValue() == 2);

                ITestInterface testBadCast = (ITestInterface)new TestInterfaceUnrelatedClass();

                tests.Add(testBadCast == null);

                return tests;
            }";

        Script newScript = ScriptParser.LexAndParseScript(
            script: TestCastingScript,
            new FunctionSignature("TestCasting", typeof(List<bool>)));

        ScriptRuntimeContext scriptContext = newScript.PrepareScript(globalContext);

        return newScript.ExecuteFunction<List<bool>>("TestCasting", 2_000, scriptContext, Array.Empty<object>());
    }

    static List<bool> TestEnums()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        ClassRegistrar.TryRegisterClass<BGC.Audio.AudioChannel>();
        ClassRegistrar.TryRegisterClass(typeof(Console));


        const string TestEnumsScript = @"
            List<bool> TestEnums()
            {
                List<bool> tests = new List<bool>();

                AudioChannel left = AudioChannel.Left;
                AudioChannel right = AudioChannel.Right;

                tests.Add(left == AudioChannel.Left);
                tests.Add(left != AudioChannel.Right);
                tests.Add(right == AudioChannel.Right);
                tests.Add(AudioChannel.Left < AudioChannel.Both);

                return tests;
            }";

        Script newScript = ScriptParser.LexAndParseScript(
            script: TestEnumsScript,
            new FunctionSignature("TestEnums", typeof(List<bool>)));

        ScriptRuntimeContext scriptContext = newScript.PrepareScript(globalContext);

        return newScript.ExecuteFunction<List<bool>>("TestEnums", 2_000, scriptContext, Array.Empty<object>());
    }

    static List<bool> TestValues()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        const string TestValuesScript = @"
            List<bool> TestValues()
            {
                List<bool> tests = new List<bool>();

                byte binaryByteTwo = 0b0000_0010;
                byte decimalByteTwo = 2;
                byte hexByteTwo = 0x02;
                tests.Add(binaryByteTwo == decimalByteTwo);
                tests.Add(hexByteTwo == decimalByteTwo);

                int intTwo = 2;
                tests.Add(intTwo == decimalByteTwo);
                tests.Add(intTwo == binaryByteTwo);
                tests.Add(intTwo == (1 << 1));


                return tests;
            }";

        Script newScript = ScriptParser.LexAndParseScript(
            script: TestValuesScript,
            new FunctionSignature("TestValues", typeof(List<bool>)));

        ScriptRuntimeContext scriptContext = newScript.PrepareScript(globalContext);

        return newScript.ExecuteFunction<List<bool>>("TestValues", 2_000, scriptContext, Array.Empty<object>());
    }

    static List<bool> TestFunctionInvocations()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        const string TestFunctionInvocationsScript = @"
            List<bool> TestFunctionInvocations()
            {
                List<bool> tests = new List<bool>();

                tests.Add(TestFunction(1) == 1);
                tests.Add(TestFunction(1.0) == 2);
                tests.Add(TestArgMatching(1) == 3);

                tests.Add(TestMultiArgMatching(1.0, 1) == 4);
                tests.Add(TestMultiArgMatching(1.0, 1.0) == 5);
                tests.Add(TestMultiArgMatching(1, 1) == 6);

                tests.Add(TestMultiArgMatching(1.0, (int)1.0) == 4);
                tests.Add(TestMultiArgMatching(1.0, 1f) == 5);
                tests.Add(TestMultiArgMatching(1, 1f) == 5);

                return tests;
            }

            int TestFunction(int value) => 1;
            int TestFunction(double value) => 2;
            int TestArgMatching(double value) => 3;

            int TestMultiArgMatching(double value, int value2) => 4;
            int TestMultiArgMatching(double value, double value2) => 5;
            int TestMultiArgMatching(int value, int value2) => 6;
            ";

        Script newScript = ScriptParser.LexAndParseScript(
            script: TestFunctionInvocationsScript,
            new FunctionSignature("TestFunctionInvocations", typeof(List<bool>)));

        ScriptRuntimeContext scriptContext = newScript.PrepareScript(globalContext);

        return newScript.ExecuteFunction<List<bool>>("TestFunctionInvocations", 2_000, scriptContext, Array.Empty<object>());
    }

    static List<bool> TestReturn()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        const string TestValuesScript = @"
            void TestReturn()
            {
                return;
            }";

        Script newScript = ScriptParser.LexAndParseScript(
            script: TestValuesScript,
            new FunctionSignature("TestReturn", typeof(void)));

        ScriptRuntimeContext scriptContext = newScript.PrepareScript(globalContext);

        newScript.ExecuteFunction("TestReturn", scriptContext, Array.Empty<object>());

        return new List<bool>() { true };
    }

    static List<bool> TestParameterArgumentModifiers()
    {
        GlobalRuntimeContext globalContext = new GlobalRuntimeContext();

        ClassRegistrar.TryRegisterClass(typeof(bool));

        const string TestArgumentsScript = @"
            List<bool> TestArgs()
            {
                List<bool> tests = new List<bool>();

                int testValue = 5;

                TestStandardArg(testValue);
                tests.Add(testValue == 5);

                TestRefArg(ref testValue);
                tests.Add(testValue == 6);

                TestOutArg(out testValue);
                tests.Add(testValue == 4);

                bool tempOut;
                tests.Add(bool.TryParse(""true"", out tempOut));
                tests.Add(tempOut);
                tests.Add(bool.TryParse(""FALSE"", out tempOut));
                tests.Add(!tempOut);

                tests.Add(!bool.TryParse(""asdf"", out tempOut));

                int testInt;
                tests.Add(int.TryParse(""5"", out testInt));
                tests.Add(testInt == 5);

                return tests;
            }


            void TestStandardArg(int value)
            {
                value++;
            }

            void TestRefArg(ref int value)
            {
                value++;
            }

            void TestOutArg(out int value)
            {
                value = 4;
            }";

        Script newScript = ScriptParser.LexAndParseScript(
            script: TestArgumentsScript,
            new FunctionSignature("TestArgs", typeof(List<bool>)));

        ScriptRuntimeContext scriptContext = newScript.PrepareScript(globalContext);

        return newScript.ExecuteFunction<List<bool>>("TestArgs", 2_000, scriptContext, Array.Empty<object>());

    }

    #region TestClasses
#pragma warning disable CA1822 // Mark members as static
    class TestGenericClass<T>
    {
        [ScriptingAccess]
        public T Value { get; set; }
        public TestGenericClass(T value)
        {
            Value = value;
        }

        [ScriptingAccess]
        public T GetValue() => Value;

        [ScriptingAccess]
        public void SetValue(T value) => Value = value;

        [ScriptingAccess]
        public Tx TryThing<Tx>(Tx input) => input;
    }

    class TestMethodOverloadingClass
    {
        [ScriptingAccess]
        public int DoThing() => 1;

        [ScriptingAccess]
        public int DoThing(int _) => 2;

        [ScriptingAccess]
        public int DoThing<T>(int _, T x) => 3;
    }

    class TestStaticBindingClass
    {
        public static List<int> List { get; } = new List<int>() { 1 };

        public static int StaticValue = 0;
        public static void IncrementStaticValue() => StaticValue++;
        public static int GetStaticValue() => StaticValue;
        public static bool CheckStaticValue(int value) => value == StaticValue;


        public int InstanceValue = 0;
        public void IncrementInstanceValue() => InstanceValue++;
        public int GetInstanceValue() => InstanceValue;
        public bool CheckInstanceValue(int value) => value == InstanceValue;
    }

    interface ITestInterface
    {
        int GetTestValue();
    }

    class TestInterface1 : ITestInterface
    {
        public int GetTestValue() => 1;
    }

    class TestInterface2 : ITestInterface
    {
        int ITestInterface.GetTestValue() => 2;
    }

    class TestInterfaceUnrelatedClass
    {
        public int GetTestValue() => 3;
    }


#pragma warning restore CA1822 // Mark members as static
    #endregion TestClasses
}