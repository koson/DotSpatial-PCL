// ********************************************************************************************************
// Product Name: DotSpatial.Topology.dll
// Description:  The basic topology module for the new dotSpatial libraries
// ********************************************************************************************************
// The contents of this file are subject to the Lesser GNU Public License (LGPL)
// you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://dotspatial.codeplex.com/license  Alternately, you can access an earlier version of this content from
// the Net Topology Suite, which is also protected by the GNU Lesser Public License and the sourcecode
// for the Net Topology Suite can be obtained here: http://sourceforge.net/projects/nts.
//
// Software distributed under the License is distributed on an "AS IS" basis, WITHOUT WARRANTY OF
// ANY KIND, either expressed or implied. See the License for the specific language governing rights and
// limitations under the License.
//
// The Original Code is from the Net Topology Suite, which is a C# port of the Java Topology Suite.
//
// The Initial Developer to integrate this code into MapWindow 6.0 is Ted Dunsford.
//
// Contributor(s): (Open source contributors should list themselves and their modifications here).
// |         Name         |    Date    |                              Comment
// |----------------------|------------|------------------------------------------------------------
// |                      |            |
// ********************************************************************************************************

using System;

namespace DotSpatial.Topology.Utilities
{
    /// <summary>
    /// A utility for making programming assertions.
    /// </summary>
    public class Assert
    {
        /// <summary>
        /// Only static methods!
        /// </summary>
        private Assert() { }

        /// <summary>
        ///
        /// </summary>
        /// <param name="assertion"></param>
        public static void IsTrue(bool assertion)
        {
            IsTrue(assertion, null);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="assertion"></param>
        /// <param name="message"></param>
        public static void IsTrue(bool assertion, string message)
        {
            if (assertion) return;
            if (message == null) throw new AssertionFailedException();
            throw new AssertionFailedException(message);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="expectedValue"></param>
        /// <param name="actualValue"></param>
        public static void IsEquals(Object expectedValue, Object actualValue)
        {
            IsEquals(expectedValue, actualValue, null);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="expectedValue"></param>
        /// <param name="actualValue"></param>
        /// <param name="message"></param>
        public static void IsEquals(Object expectedValue, Object actualValue, string message)
        {
            if (!actualValue.Equals(expectedValue))
                throw new AssertionFailedException("Expected " + expectedValue + " but encountered "
                            + actualValue + (message != null ? ": " + message : String.Empty));
        }
    }
}