using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Tools.Compiler.Native
{
    public class ILCompiler
    {
        private List<ILCompilationStep> StepList { get; set; }

        private ILCompiler(List<ILCompilationStep> compilationSteps)
        {
            StepList = compilationSteps;
        }

        public static ILCompiler Create(NativeCompileSettings config)
        {
            var compilationStepList = CreateCompilationSteps(config);
            return new ILCompiler(compilationStepList);
        }
        
        private static List<ILCompilationStep> CreateCompilationSteps(NativeCompileSettings config)
        {
            List<ILCompilationStep> compilationSteps = new List<ILCompilationStep>();
            if (config.IsMultiModuleBuild)
            {
                compilationSteps.Add(new IlcSdkCompilationStep(config));
                compilationSteps.Add(new AppCompilationStep(config));
            }
            else
            {
                compilationSteps.Add(new AppCompilationStep(config));
            }

            return compilationSteps;
        }

        public int Invoke()
        {
            foreach (var step in StepList)
            {
                if (step.OutputIsUpToDate)
                {
                    Reporter.Verbose.WriteLine($"Skipping {step.GetType().Name} because output is up-to-date with respect to its inputs.");
                    continue;
                }

                int result = step.Invoke();

                if (result != 0)
                {
                    return result;
                }
            }

            return 0;
        }
    }
}
