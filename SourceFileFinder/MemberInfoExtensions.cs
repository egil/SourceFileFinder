using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReflectionHelpers
{
    /// <summary>
    /// Extensions for <see cref="MemberInfo"/>.
    /// Copied from https://source.dot.net/#System.Reflection.TypeExtensions/System/Reflection/TypeExtensions.cs,277.
    /// </summary>
    public static class MemberInfoExtensions
    {

        /// <summary>
        /// Determines if there is a metadata token available for the given member.
        /// <see cref="GetMetadataToken(MemberInfo)"/> throws <see cref="InvalidOperationException"/> otherwise.
        /// </summary>
        /// <remarks>This maybe</remarks>
        public static bool HasMetadataToken(this MemberInfo member)
        {
            if (member is null)
                throw new ArgumentNullException(nameof(member));
            try
            {
                return GetMetadataTokenOrZeroOrThrow(member) != 0;
            }
            catch (InvalidOperationException)
            {
                // Thrown for unbaked ref-emit members/types.
                // Other cases such as typeof(byte[]).MetadataToken will be handled by comparison to zero above.
                return false;
            }
        }

        /// <summary>
        /// Gets a metadata token for the given member if available. The returned token is never nil.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// There is no metadata token available. <see cref="HasMetadataToken(MemberInfo)"/> returns false in this case.
        /// </exception>
        public static int GetMetadataToken(this MemberInfo member)
        {
            if (member is null)
                throw new ArgumentNullException(nameof(member));
            int token = GetMetadataTokenOrZeroOrThrow(member);

            if (token == 0)
            {
                throw new InvalidOperationException("There is no metadata token available for the given member.");
            }

            return token;
        }

        private static int GetMetadataTokenOrZeroOrThrow(MemberInfo member)
        {
            int token = member.MetadataToken;

            // Tokens have MSB = table index, 3 LSBs = row index
            // row index of 0 is a nil token
            const int rowMask = 0x00FFFFFF;
            if ((token & rowMask) == 0)
            {
                // Nil token is returned for edge cases like typeof(byte[]).MetadataToken.
                return 0;
            }

            return token;
        }
    }
}
