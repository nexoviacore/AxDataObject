using Microsoft.Extensions.Options;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace ARMCommon.Helpers
{
    public sealed class AESPasswordHelper
    {
        public const string SectionName = "AesCompat";
        public string SymKey { get; set; } = "22723BBD4217A0ABF6D3E68073C7603D";
        public string TextEncoding { get; set; } = "UTF-8";        
        public bool TrimDecryptionUsingHeader { get; set; } = true;

        private const int AesBlockSizeBytes = 16;
        private const int HeaderSizeBytes = 4;
        private readonly Encoding _textEncoding;
        private readonly byte[] _aes128Key;

        public AESPasswordHelper()
        {
            _textEncoding = ResolveEncoding(TextEncoding);
            _aes128Key = BuildDelphiAes128Key(SymKey);
        }

        public string EncryptText(string text)
        {
            try
            {
                var plainBytes = _textEncoding.GetBytes(text);
                return EncryptBytesToHex(plainBytes);
            }
            catch { }
            return text;
        }

        public string DecryptText(string encryptedHex)
        {
            try
            {
                var plainBytes = DecryptHexToBytes(encryptedHex);
                return _textEncoding.GetString(plainBytes);
            }
            catch { }
            return encryptedHex;
        }

        public string EncryptBytesToHex(ReadOnlySpan<byte> plainBytes)
        {
            Span<byte> header = stackalloc byte[HeaderSizeBytes];
            BinaryPrimitives.WriteInt32LittleEndian(header, plainBytes.Length);

            var paddedPlain = ZeroPadToAesBlock(plainBytes);
            var cipherBytes = paddedPlain.Length == 0
                ? Array.Empty<byte>()
                : TransformEcbNoPadding(paddedPlain, encrypt: true);

            var output = new byte[HeaderSizeBytes + cipherBytes.Length];
            header.CopyTo(output);
            cipherBytes.CopyTo(output.AsSpan(HeaderSizeBytes));

            // Delphi StrToHex uses IntToHex(byte, 2), which produces uppercase hex.
            return Convert.ToHexString(output);
        }

        public byte[] DecryptHexToBytes(string encryptedHex)
        {
            var rawPadded = DecryptHexToRawPaddedBytes(encryptedHex);

            if (!TrimDecryptionUsingHeader)
                return rawPadded.PaddedPlainBytes;

            if (rawPadded.OriginalSize > rawPadded.PaddedPlainBytes.Length)
                throw new Exception($"Invalid encrypted payload: header size {rawPadded.OriginalSize} exceeds decrypted size {rawPadded.PaddedPlainBytes.Length}.");

            var result = new byte[rawPadded.OriginalSize];
            Buffer.BlockCopy(rawPadded.PaddedPlainBytes, 0, result, 0, result.Length);
            return result;
        }
    

        private (int OriginalSize, byte[] PaddedPlainBytes) DecryptHexToRawPaddedBytes(string encryptedHex)
        {
            if (string.IsNullOrWhiteSpace(encryptedHex))
                throw new Exception("Encrypted value is required.");

            byte[] payload;
            try
            {
                payload = Convert.FromHexString(encryptedHex.Trim());
            }
            catch (FormatException ex)
            {
                throw new Exception("Encrypted value must be an even-length hexadecimal string.", ex);
            }

            if (payload.Length < HeaderSizeBytes)
                throw new Exception("Encrypted payload is too short. Expected at least a 4-byte size header.");

            var originalSize = BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(0, HeaderSizeBytes));
            if (originalSize < 0)
                throw new Exception("Encrypted payload contains a negative original-size header.");

            var cipherLength = payload.Length - HeaderSizeBytes;
            if (cipherLength == 0)
            {
                if (originalSize != 0)
                    throw new Exception("Encrypted payload has no AES blocks but header size is not zero.");
                return (originalSize, Array.Empty<byte>());
            }

            if (cipherLength % AesBlockSizeBytes != 0)
                throw new Exception("Invalid encrypted payload. Ciphertext length after the 4-byte header must be a multiple of 16.");

            var cipherBytes = payload.AsSpan(HeaderSizeBytes).ToArray();
            var plainPadded = TransformEcbNoPadding(cipherBytes, encrypt: false);
            return (originalSize, plainPadded);
        }

        private byte[] TransformEcbNoPadding(byte[] input, bool encrypt)
        {
            using var aes = Aes.Create();
            aes.KeySize = 128;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;
            aes.Key = _aes128Key;

            using var transform = encrypt ? aes.CreateEncryptor() : aes.CreateDecryptor();
            return transform.TransformFinalBlock(input, 0, input.Length);
        }

        private static byte[] ZeroPadToAesBlock(ReadOnlySpan<byte> plainBytes)
        {
            if (plainBytes.Length == 0)
                return Array.Empty<byte>();

            var paddedLength = checked(((plainBytes.Length + AesBlockSizeBytes - 1) / AesBlockSizeBytes) * AesBlockSizeBytes);
            var padded = new byte[paddedLength];
            plainBytes.CopyTo(padded);
            return padded;
        }

        private static byte[] BuildDelphiAes128Key(string symKey)
        {
            if (string.IsNullOrEmpty(symKey))
                throw new Exception("SymKey must not be empty.");

            var ascii = Encoding.ASCII.GetBytes(symKey);
            var key = new byte[AesBlockSizeBytes];
            Buffer.BlockCopy(ascii, 0, key, 0, Math.Min(key.Length, ascii.Length));
            return key;
        }

        private static Encoding ResolveEncoding(string encodingName)
        {
            if (string.IsNullOrWhiteSpace(encodingName))
                return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

            if (encodingName.Equals("UTF-8", StringComparison.OrdinalIgnoreCase) ||
                encodingName.Equals("UTF8", StringComparison.OrdinalIgnoreCase))
            {
                return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            }

            try
            {
                return Encoding.GetEncoding(encodingName, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
            {
                throw new Exception($"Unsupported text encoding '{encodingName}'.", ex);
            }
        }

    }
}
