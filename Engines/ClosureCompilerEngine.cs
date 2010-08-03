﻿using System.Collections.Generic;
using System.Linq;

namespace Zippy.Chirp.Engines {
    class ClosureCompilerEngine : BasicEngine<ClosureCompilerEngine> {
        public ClosureCompilerEngine() : base(new[] { Settings.ChirpGctJsFile, Settings.ChirpWhiteSpaceJsFile, Settings.ChirpSimpleJsFile }, new[] { ".min.js" }) { }

        public override IEnumerable<IResult> BasicTransform(Item item)
        {
            var mode = item.FileName.EndsWith(Settings.ChirpGctJsFile, System.StringComparison.OrdinalIgnoreCase) ? ClosureCompilerCompressMode.ADVANCED_OPTIMIZATIONS
                : item.FileName.EndsWith(Settings.ChirpSimpleJsFile, System.StringComparison.OrdinalIgnoreCase) ? ClosureCompilerCompressMode.SIMPLE_OPTIMIZATIONS
                : ClosureCompilerCompressMode.WHITESPACE_ONLY;

            return BasicTransform(item, mode);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public IEnumerable<IResult> BasicTransform(Item item, ClosureCompilerCompressMode mode)
        {
           
            var reporter = new EcmaScriptErrorReporter(item.FileName);

            string returnedCode = null;

            try
            {
                returnedCode = GoogleClosureCompiler.Compress(item.FileName, mode, (category, msg, line, col) =>
                {
                    reporter.Error(msg, item.FileName, line, string.Empty, col);
                });
            }
            catch (System.Exception) { }

            if (reporter.Errors.Any()) foreach (var err in reporter.Errors) yield return err;
            else yield return new FileResult(item, ".min.js", returnedCode, true);
        }

    }
}
