﻿using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using CommandLine;

namespace Pixie
{

    class Program
    {
        private static string _invokedVerb;
        private static CommonOptions _suboptions;

        static int Main(string[] args)
        {
            var options = new CommandLineOptions();
            
            if (!Parser.Default.ParseArguments(args, options,
                (verb, suboptions) =>
                {
                    _invokedVerb = verb;
                    _suboptions = (CommonOptions)suboptions;
                }))
            {
                Environment.Exit((int)ErrorCode.ArgumentsMismatch);
            }


            int errorCode = (int) ErrorCode.NoError;
            if (_invokedVerb == "generate")
            {
                errorCode = GeneratePattern((GenerateOptions) _suboptions);
            }
            if (_invokedVerb == "parse")
            {
                errorCode = ParseFontImage((ParseOptions) _suboptions);
            }

            if(errorCode == (int)ErrorCode.NoError)
                ConsoleLogger.WriteMessage($"SUCCESS! \nFile written: \"{_suboptions.OutputFileName}\"", MessageType.Info);
            return errorCode;
        }

        /// <summary>
        /// parses image and write arrays to output file
        /// </summary>
        /// <param name="options">parsed command line args</param>
        /// <returns>error code</returns>
        static int ParseFontImage(ParseOptions options)
        {
            try
            {
                var bitmap = new Bitmap(Image.FromFile(options.InputFileName));
                var settings = PixelSettings.FromFile(options.PixelSettingsPath);
                var mapper = new PixelMapper(bitmap, settings);
                var map = mapper.MapPixels(options.SkipHeaders);
                OutputFileFormatter.WriteOutput(map, options.OutputFileName, options.SingleArray);
            }
            catch (Exception e)
            {
                if (e is FileNotFoundException)
                {
                    ConsoleLogger.WriteMessage($"File not found or invalid file name \"{e.Message}\"", MessageType.Error);
                    return (int)ErrorCode.FileNotFound;
                }

                ConsoleLogger.WriteMessage(e.Message + "\n" + e.InnerException?.Message, MessageType.Error);

                if (e is ArgumentException)
                {
                   return (int)ErrorCode.FileParsingError;
                }
               
                return (int)ErrorCode.UknownError;
            }
            return (int) ErrorCode.NoError;
        }

        /// <summary>
        /// Grid pattern generation and writing to file
        /// </summary>
        /// <param name="options">parsed command line args</param>
        /// <returns>error code</returns>
        static int GeneratePattern(GenerateOptions options)
        {
            try
            {
                var settings = PixelSettings.FromFile(options.PixelSettingsPath);
                var generator = new PatternGenerator(settings);
                var pattern = generator.GeneratePattern(options.PatternWidth, options.PatternHeight, options.EnumerationStyle);
                pattern.Save(options.OutputFileName);
            }
            catch (Exception e)
            {
                ConsoleLogger.WriteMessage(e.Message + "\n" + e.InnerException?.Message, MessageType.Error);
                if (e is FileNotFoundException)
                    return (int)ErrorCode.FileNotFound;
                return (int)ErrorCode.UknownError;
            }
            return (int)ErrorCode.NoError;
        }

    }
}
