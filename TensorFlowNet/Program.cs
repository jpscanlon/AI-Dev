using System;
using static Tensorflow.Binding;
using NumSharp;
using Tensorflow;

namespace TensorFlowNET.Examples
{
    /// <summary>
    /// Simple hello world using TensorFlow
    /// </summary>
    public class HelloWorld  // : IExample
    {
        public static void Main(string[] args)
        {
            /* Create a Constant op
               The op is added as a node to the default graph.
            
               The value returned by the constructor represents the output
               of the Constant op. */
            var hello = tf.constant("Hello, TensorFlow!");

            // Start tf session
            using var sess = tf.Session();
            // Run the op
            var result = sess.run(hello);

            string resultString;
            resultString = result.ToString();

            //Console.WriteLine(result);
            Console.WriteLine(resultString);
            Console.ReadLine();
        }
    }
}