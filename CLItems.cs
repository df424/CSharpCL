using System;

namespace CLLib
{
    /// <summary>
    /// Use this class to set commands in a command line application.
    /// The commanst must be public static functions in the class that is passed
    /// to the Execute<> function.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CLCommand : Attribute
    {
        // The string that will call the given command.
        public string CommandString { get; private set; }
        // The help string that will be displayed for this command.
        public string HelpString { get; set; }

        public string UsageString { get; set; }

        public CLCommand(string command_string)
        {
            if (string.IsNullOrWhiteSpace(command_string))
            {
                throw new ArgumentException("Command names cannot be whitespace", nameof(command_string));
            }

            CommandString = command_string;
        }
    }

    /// <summary>
    /// Use this class to set parameters in a command line application.
    /// The parameters must be public static properties in the class that is passed
    /// to the Execute<> function.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class CLParameter : Attribute
    {
        /// <summary>
        /// The string that will preceed the setting of parameters.
        /// </summary>
        public string ParameterString { get; private set; }

        /// <summary>
        /// The help string that will be displayed for this parameter.
        /// </summary>
        public string HelpString { get; set; }

        /// <summary>
        /// True if this is a boolean toggle.  If it is a boolean toggle it will be
        /// assigned true if "ParameterString" is pressent in the system arguments.
        /// </summary>
        public bool IsBool { get; set; }

        /// <summary>
        /// True if the parameter was actually set when the program was called.
        /// false otherwise
        /// </summary>
        public bool WasSet { get; set; }

        /// <summary>
        /// List of all commands this parameter is used for.
        /// </summary>
        public string[] CommandList { get; set; }

        /// <summary>
        /// Construct a CLParameter.  The string must be provided.
        /// </summary>
        /// <param name="parameter_string">The name of the parameter a '-' is already included</param>
        public CLParameter(string parameter_string)
        {
            if (string.IsNullOrWhiteSpace(parameter_string))
            {
                throw new ArgumentException("Parameter names cannot be whitespace.", nameof(parameter_string));
            }

            ParameterString = parameter_string;
        }
    } // End Class
} // End Namespace
