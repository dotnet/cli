using System;

namespace Microsoft.DotNet.Tools.Compiler.Native
{
	public class NativeCompiler 
	{
		public static NativeCompiler Create(NativeCompileSettings config)
		{
            var msilCompiler = ILCompiler.Create(config);
			var intCompiler = IntermediateCompiler.Create(config);

            var nc = new NativeCompiler()
            {
                ilCompiler = msilCompiler,
                intermediateCompiler = intCompiler
			};
			
			return nc;
		}

        private ILCompiler ilCompiler;
        private IntermediateCompiler intermediateCompiler;

		public bool CompileToNative(NativeCompileSettings config)
		{	
			int result = ilCompiler.Invoke();
            if(result != 0)
            {
                return false;
            }

            result = intermediateCompiler.Invoke();
            if (result != 0)
            {
                return false;
            }

            return true;
		}
	}
}