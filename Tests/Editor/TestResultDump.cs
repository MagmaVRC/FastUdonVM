using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace MagmaVRC.UdonVM.Tests
{
    /// <summary>Writes every failed test of a run to Library/udonvm-testresults.txt.</summary>
    [InitializeOnLoad]
    static class TestResultDump
    {
        sealed class Callbacks : ICallbacks
        {
            readonly StringBuilder _sb = new StringBuilder();
            int _passed, _failed, _skipped;

            public void RunStarted(ITestAdaptor testsToRun)
            {
                _sb.Clear();
                _passed = _failed = _skipped = 0;
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                _sb.Insert(0, $"passed={_passed} failed={_failed} skipped={_skipped}\n");
                File.WriteAllText("Library/udonvm-testresults.txt", _sb.ToString());
            }

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.HasChildren) return;
                if (result.TestStatus == TestStatus.Passed) _passed++;
                else if (result.TestStatus == TestStatus.Failed)
                {
                    _failed++;
                    _sb.Append("FAIL ").Append(result.FullName).Append('\n').Append(result.Message).Append('\n').Append(result.StackTrace).Append("\n\n");
                }
                else _skipped++;
            }
        }

        static TestResultDump()
        {
            ScriptableObject.CreateInstance<TestRunnerApi>().RegisterCallbacks(new Callbacks());
        }
    }
}
