using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CLLib
{
    public class CLProgram
    {
        static Type t;
        static Dictionary<string, MethodInfo> _Methods = new Dictionary<string, MethodInfo>();
        static Dictionary<string, CLCommand> _MethodAttributes = new Dictionary<string, CLCommand>();
        static Dictionary<string, PropertyInfo> _Properties = new Dictionary<string, PropertyInfo>();
        static Dictionary<string, CLParameter> _ParameterAttributes = new Dictionary<string, CLParameter>();

        public static void Execute<T>(string[] args, string[] help_flags = null)
        {
            // If the user gave no argus print the usage
            if(args.Length < 1)
            {
                Console.WriteLine("Usage: " + System.AppDomain.CurrentDomain.FriendlyName + " [COMMAND]... [PARAMETER] VALUE... [FLAG]...");
                Console.WriteLine("Try \"" + System.AppDomain.CurrentDomain.FriendlyName + " --help\" for more information.");
            }

            // If the user provided no help_flags use the default help_flags.
            if(help_flags == null)
                help_flags = new[] { "--help", "-h" };

            // Store the type of the main program.
            t = typeof(T);

            SetupMethodDictionary();
            SetupPropertyDictionary();

            string command = null;
            bool property_value_next = false;
            PropertyInfo property_info = null;
            

            foreach(var a in args)
            {
                // Check for any of the help flags.
                if(help_flags.Contains(a))
                {
                    // If this is a help flag just print the help and return.
                    PrintHelp(command);
                    return;
                }

                if(property_value_next)
                {
                    TryParameterParse(property_info, a);
                    property_value_next = false;
                }
                else if(a[0] == '-')
                {
                    // If the property exists.
                    if(_Properties.ContainsKey(a.Substring(1)))
                    {
                        // If this a boolean toggle parameter we just switch its value.
                        if (_Properties[a.Substring(1)].GetCustomAttribute<CLParameter>().IsBool)
                        {
                            _Properties[a.Substring(1)].SetValue(property_info, true); 
                        }
                        else // Otherwise we set things up to read the new value from the next argument.
                        {
                            property_value_next = true;
                            property_info = _Properties[a.Substring(1)];
                        }
                    }
                    else
                    {
                        Console.WriteLine("ERROR: Unrecognized parameter \"" + a + "\".");
                    }
                }
                else // If the first character is not a '-' it is a command.
                {
                    // There can only be 1 command so if this is the second one that is a problem.
                    if(command != null)
                    {
                        Console.WriteLine("ERROR: Multiple commands are not supported.  Encountered second command \"" + a + "\".");
                        return;
                    }
                    else
                    {
                        // Try to find it in the dictionary of commands.
                        if(_Methods.ContainsKey(a))
                        {
                            // The method exists so we store it in the command string.
                            command = a;
                        }
                        else
                        {
                            Console.WriteLine("ERROR: Specified command \"" + a + "\" is not implemented.");
                            return;
                        }
                    }
                }
            }

            // At this point if there is a command it is okay to call it so we just do it.
            if(command != null)
            {
                _Methods[command].Invoke(null, null);
            }
        }

        public static CLCommand GetCommandAttribute(string command_string)
        {
            if (_Methods.ContainsKey(command_string))
                return _Methods[command_string].GetCustomAttribute<CLCommand>();

            return null;
        }

        public static CLParameter GetParameterAttribute(string parameter_string)
        {
            if (_ParameterAttributes.ContainsKey(parameter_string))
                return _ParameterAttributes[parameter_string];

            return null;
        }

        static void SetupMethodDictionary()
        {
            // Get all the method infos and turn them into a dictionary.
            _Methods = t.GetTypeInfo().DeclaredMethods
                .Where(m => m.GetCustomAttribute<CLCommand>() != null)
                .ToDictionary(m => m.GetCustomAttribute<CLCommand>().CommandString);

            // Using the newly made dictionary store all the CLCommand structures for further
            // access and so they can be made modifiable.
            foreach (var val in _Methods)
            {
                _MethodAttributes.Add(val.Key, val.Value.GetCustomAttribute<CLCommand>());
            }

        }

        static void SetupPropertyDictionary()
        {
            // We get all of the property infos and turn them into a dictionary.
            _Properties = t.GetTypeInfo().DeclaredProperties
                .Where(p => p.GetCustomAttribute<CLParameter>() != null)
                .ToDictionary(p => p.GetCustomAttribute<CLParameter>().ParameterString);

            // Likewise we create an instance of the CLParameter attributes so that we have
            // a modifiable value.
            foreach(var val in _Properties)
            {
                _ParameterAttributes.Add(val.Key, val.Value.GetCustomAttribute<CLParameter>());
            }
        }

        static void TryParameterParse(PropertyInfo info, string raw_value)
        {
            // Use the parameter info to get the copy of our custom attribute by getting the parameter string as the key.
            // we don't need to check if it exists because at this point we have guaranteed that it will.
            CLParameter param_attr = _ParameterAttributes[info.GetCustomAttribute<CLParameter>().ParameterString];
            object final_value = null;

            if (info.PropertyType == typeof(Boolean))
            {
                Boolean value;
                if (!Boolean.TryParse(raw_value, out value))
                {
                    Console.WriteLine("ERROR: Bad parameter value. ParameterName=\"" + param_attr.ParameterString + "\" Value=\"" + raw_value + "\"");
                    Console.WriteLine("Value must be boolean value (e.g. true/false)");
                    return;
                }
                final_value = value;
            }

            if (info.PropertyType == typeof(Byte))
            {
                Byte value;
                if (!Byte.TryParse(raw_value, out value))
                {
                    Console.WriteLine("ERROR: Bad parameter value. ParameterName=\"" + param_attr.ParameterString + "\" Value=\"" + raw_value + "\"");
                    Console.WriteLine("Value must be an unsigned byte value [0:255]");
                    return;
                }
                final_value = value;
            }

            if (info.PropertyType == typeof(SByte))
            {
                SByte value;
                if(!SByte.TryParse(raw_value, out value))
                {

                    Console.WriteLine("ERROR: Bad parameter value. ParameterName=\"" + param_attr.ParameterString + "\" Value=\"" + raw_value + "\"");
                    Console.WriteLine("Value must be a signed byte value [-128:127]");
                    return;
                }

                final_value = value;
            }

            if (info.PropertyType == typeof(Int16))
            {
                Int16 value;
                if(!Int16.TryParse(raw_value, out value))
                {
                    Console.WriteLine("ERROR: Bad parameter value. ParameterName=\"" + param_attr.ParameterString + "\" Value=\"" + raw_value + "\"");
                    Console.WriteLine("Value must be a 16-bit signed integer");
                    return;
                }

                final_value = value;
            }

            if (info.PropertyType == typeof(UInt16))
            {
                UInt16 value;
                if(!UInt16.TryParse(raw_value, out value))
                {
                    Console.WriteLine("ERROR: Bad parameter value. ParameterName=\"" + param_attr.ParameterString + "\" Value=\"" + raw_value + "\"");
                    Console.WriteLine("Value must be a 16-bit unsigned integer");
                    return;
                }

                final_value = value;
            }

            if(info.PropertyType == typeof(Int32))
            {
                Int32 value;
                if(!Int32.TryParse(raw_value, out value))
                {
                    Console.WriteLine("ERROR: Bad parameter value. ParameterName=\"" + param_attr.ParameterString + "\" Value=\"" + raw_value + "\"");
                    Console.WriteLine("Value must be a 32-bit signed integer");
                    return;
                }

                final_value = value;
            }
                            
            if (info.PropertyType == typeof(UInt32))
            {
                UInt32 value;
                if(!UInt32.TryParse(raw_value, out value))
                {
                    Console.WriteLine("ERROR: Bad parameter value. ParameterName=\"" + param_attr.ParameterString + "\" Value=\"" + raw_value + "\"");
                    Console.WriteLine("Value must be a 32-bit unsigned integer");
                    return;
                }

                final_value = value;
            }

            if (info.PropertyType == typeof(Int64))
            {
                Int64 value;
                if(!Int64.TryParse(raw_value, out value))
                {
                    Console.WriteLine("ERROR: Bad parameter value. ParameterName=\"" + param_attr.ParameterString + "\" Value=\"" + raw_value + "\"");
                    Console.WriteLine("Value must be a 64-bit signed integer");
                    return;
                }

                final_value = value;
            }

            if (info.PropertyType == typeof(UInt64))
            {
                UInt64 value;
                if(!UInt64.TryParse(raw_value, out value))
                {
                    Console.WriteLine("ERROR: Bad parameter value. ParameterName=\"" + param_attr.ParameterString + "\" Value=\"" + raw_value + "\"");
                    Console.WriteLine("Value must be a 64-bit unsigned integer");
                    return;
                }

                final_value = value;

            }

            if(info.PropertyType == typeof(Single))
            {
                Single value;
                if(!Single.TryParse(raw_value, out value))
                {
                    Console.WriteLine("ERROR: Bad parameter value. ParameterName=\"" + param_attr.ParameterString + "\" Value=\"" + raw_value + "\"");
                    Console.WriteLine("Value must be a floating point number(e.g. 1.0)");
                    return;
                }
                final_value = value;
            }

            if(info.PropertyType == typeof(Double))
            {
                Double value;
                if(!Double.TryParse(raw_value, out value))
                {
                    Console.WriteLine("ERROR: Bad parameter value. ParameterName=\"" + param_attr.ParameterString + "\" Value=\"" + raw_value + "\"");
                    Console.WriteLine("Value must be a floating point number(e.g. 1.0)");
                    return;
                }
                final_value = value;
            }

            // If it is a string just copy it into the final_value.
            if(info.PropertyType == typeof(string))
            {
                final_value = raw_value;
            }

            // If we made it to here that means the parameter has been successfully set.
            // Update the stored attributes.
            info.SetValue(info, final_value);
            param_attr.WasSet = true;
        }

        static void PrintHelp(string command)
        {
            // If the command is null print general help.
            if(command == null)
            {
                Console.WriteLine("\nThe following commands are available in this program: \n");
                foreach(var value in _MethodAttributes)
                {
                    var command_name_string = "        " + value.Key + " - ";

                    // Print the commands name.
                    Console.Write(command_name_string);

                    PrettyPrintMultiLineString(command_name_string.Length, value.Value.HelpString);
                }
                Console.WriteLine("\n        Try \"" + AppDomain.CurrentDomain.FriendlyName + " [COMMAND] --help\" for more information on individual commands.\n");
            }
            else // Otherwise print general help.
            {
                // Collect all of the parameters that are marked with the given command.
                var param_dict = _ParameterAttributes.Where(attr => attr.Value.CommandList != null && attr.Value.CommandList.Contains(command));
                // Collect a reference to the command attributes.
                var command_attr = _MethodAttributes[command];

                // Print the command and its help string.
                Console.WriteLine("\n" + Indent(4) + "[" + command_attr.CommandString + "]\n");
                PrettyPrintMultiLineString(8, command_attr.HelpString, false);
                
                foreach(var val in param_dict)
                {
                    // Split the help string into its new lines so we can format it.
                    string[] lines = new string[] { };

                    // If the help string is implemented...
                    if(val.Value.HelpString != null)
                        lines = val.Value.HelpString.Split('\n');

                    // Print the parameter name.
                    string arg_string = Indent(8) + "-" + val.Key + " - ";
                    Console.Write(arg_string);

                    PrettyPrintMultiLineString(arg_string.Length, val.Value.HelpString);
                }
            }
        }

        static void PrettyPrintMultiLineString(int base_offset, string s, bool indent_first_line = true)
        {
            // Default to zero lines since we wan't to print onthing if s is null.
            string[] lines = new string[] { };

            // Only try to split s if it isn't null.
            if(s != null)
                lines = s.Split('\n');

            // Print all of the lines of hte help string.
            foreach(var line in lines)
            {
                // If its the first line don't add the indentation.
                if (Array.IndexOf(lines, line) == 0 && indent_first_line)
                    Console.Write(line + "\n");
                else
                    Console.Write(Indent(base_offset) + line + "\n");
            }

            // if there was no lines we need an extra new line..
            if (lines.Length == 0)
                Console.Write("\n");

            Console.Write("\n");
        }

        static string Indent(int count)
        {
            return "".PadLeft(count);
        }
    }
}
