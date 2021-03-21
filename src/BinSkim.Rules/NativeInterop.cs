// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using _FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace Microsoft.CodeAnalysis.IL.Rules
{
    internal static class Native
    {
        public static readonly Guid ActionGenericVerifyV2 = new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        public enum WinVerifyTrustErrors : uint
        {
            TRUST_E_NOSIGNATURE = 0x800B0100,
            CERT_E_EXPIRED = 0x800B0101,
            CERT_E_VALIDITYPERIODNESTING = 0x800B0102,
            CERT_E_ROLE = 0x800B0103,
            CERT_E_PATHLENCONST = 0x800B0104,
            CERT_E_CRITICAL = 0x800B0105,
            CERT_E_PURPOSE = 0x800B0106,
            CERT_E_ISSUERCHAINING = 0x800B0107,
            CERT_E_MALFORMED = 0x800B0108,
            CERT_E_UNTRUSTEDROOT = 0x800B0109,
            CERT_E_CHAINING = 0x800B010A,
            TRUST_E_Error = 0x800B010B,
            CERT_E_REVOKED = 0x800B010C,
            CERT_E_UNTRUSTEDTESTROOT = 0x800B010D,
            CERT_E_REVOCATION_ErrorURE = 0x800B010E,
            CERT_E_CN_NO_MATCH = 0x800B010F,
            CERT_E_WRONG_USAGE = 0x800B0110,
            TRUST_E_EXPLICIT_DISTRUST = 0x800B0111,
            CERT_E_UNTRUSTEDCA = 0x800B0112,
            CERT_E_INVALID_POLICY = 0x800B0113,
            CERT_E_INVALID_NAME = 0x800B0114,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct CERT_CHAIN_CONTEXT
        {
            internal uint cbSize;
            internal uint dwErrorStatus;   // serialized CERT_TRUST_STATUS
            internal uint dwInfoStatus;    // serialized CERT_TRUST_STATUS
            internal uint cChain;
            internal IntPtr rgpChain;                    // PCERT_SIMPLE_CHAIN*
            internal uint cLowerQualityChainContext;
            internal IntPtr rgpLowerQualityChainContext; // PCCERT_CHAIN_CONTEXT*
            internal uint fHasRevocationFreshnessTime;
            internal uint dwRevocationFreshnessTime;     // seconds
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CERT_SIMPLE_CHAIN
        {
            internal uint cbSize;
            internal uint dwErrorStatus;   // serialized CERT_TRUST_STATUS
            internal uint dwInfoStatus;    // serialized CERT_TRUST_STATUS
            internal uint cElement;
            internal IntPtr rgpElement;      // PCERT_CHAIN_ELEMENT*
            internal IntPtr pTrustListInfo;
            internal uint fHasRevocationFreshnessTime;
            internal uint dwRevocationFreshnessTime;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CERT_CHAIN_ELEMENT
        {
            internal uint cbSize;
            internal IntPtr pCertContext;
            internal uint dwErrorStatus;   // serialized CERT_TRUST_STATUS
            internal uint dwInfoStatus;    // serialized CERT_TRUST_STATUS
            internal IntPtr pRevocationInfo;
            internal IntPtr pIssuanceUsage;
            internal IntPtr pApplicationUsage;
            internal IntPtr pwszExtendedErrorInfo;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CERT_CONTEXT
        {
            internal uint dwCertEncodingType;
            internal IntPtr pbCertEncoded;
            internal uint cbCertEncoded;
            internal IntPtr pCertInfo;
            internal IntPtr hCertStore;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CERT_INFO
        {
            internal uint dwVersion;
            internal CRYPTOAPI_BLOB SerialNumber;
            internal CRYPT_ALGORITHM_IDENTIFIER SignatureAlgorithm;
            internal CRYPTOAPI_BLOB Issuer;
            internal _FILETIME NotBefore;
            internal _FILETIME NotAfter;
            internal CRYPTOAPI_BLOB Subject;
            internal CERT_PUBLIC_KEY_INFO SubjectPublicKeyInfo;
            internal CRYPT_BIT_BLOB IssuerUniqueId;
            internal CRYPT_BIT_BLOB SubjectUniqueId;
            internal uint cExtension;
            internal IntPtr rgExtension; // PCERT_EXTENSION
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CERT_PUBLIC_KEY_INFO
        {
            internal CRYPT_ALGORITHM_IDENTIFIER Algorithm;
            internal CRYPT_BIT_BLOB PublicKey;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CRYPT_ALGORITHM_IDENTIFIER
        {
            [MarshalAs(UnmanagedType.LPStr)]
            internal string pszObjId;
            internal CRYPTOAPI_BLOB Parameters;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CRYPT_ALGORITHM_IDENTIFIER2
        {
            internal IntPtr pszObjId;
            internal CRYPTOAPI_BLOB Parameters;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CRYPT_ATTRIBUTE
        {
            [MarshalAs(UnmanagedType.LPStr)]
            internal string pszObjId;
            internal uint cValue;
            internal IntPtr rgValue;    // PCRYPT_ATTR_BLOB
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CRYPT_ATTRIBUTES
        {
            internal uint cAttr;
            internal IntPtr rgAttr;     // PCRYPT_ATTRIBUTE
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CRYPT_ATTRIBUTE_TYPE_VALUE
        {
            [MarshalAs(UnmanagedType.LPStr)]
            internal string pszObjId;
            internal CRYPTOAPI_BLOB Value;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CRYPT_BIT_BLOB
        {
            internal uint cbData;
            internal IntPtr pbData;
            internal uint cUnusedBits;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CRYPTOAPI_BLOB
        {
            internal uint cbData;
            internal IntPtr pbData;
        }

        // Algorithm types
        internal const uint ALG_TYPE_ANY = (0);
        internal const uint ALG_TYPE_DSS = (1 << 9);
        internal const uint ALG_TYPE_RSA = (2 << 9);
        internal const uint ALG_TYPE_BLOCK = (3 << 9);
        internal const uint ALG_TYPE_STREAM = (4 << 9);
        internal const uint ALG_TYPE_DH = (5 << 9);
        internal const uint ALG_TYPE_SECURECHANNEL = (6 << 9);

        // Algorithm classes
        internal const uint ALG_CLASS_ANY = (0);
        internal const uint ALG_CLASS_SIGNATURE = (1 << 13);
        internal const uint ALG_CLASS_MSG_ENCRYPT = (2 << 13);
        internal const uint ALG_CLASS_DATA_ENCRYPT = (3 << 13);
        internal const uint ALG_CLASS_HASH = (4 << 13);
        internal const uint ALG_CLASS_KEY_EXCHANGE = (5 << 13);
        internal const uint ALG_CLASS_ALL = (7 << 13);

        // Hash sub ids
        internal const uint ALG_SID_MD2 = 1;
        internal const uint ALG_SID_MD4 = 2;
        internal const uint ALG_SID_MD5 = 3;
        internal const uint ALG_SID_SHA = 4;
        internal const uint ALG_SID_SHA1 = 4;
        internal const uint ALG_SID_MAC = 5;
        internal const uint ALG_SID_RIPEMD = 6;
        internal const uint ALG_SID_RIPEMD160 = 7;
        internal const uint ALG_SID_SSL3SHAMD5 = 8;
        internal const uint ALG_SID_HMAC = 9;
        internal const uint ALG_SID_TLS1PRF = 10;
        internal const uint ALG_SID_HASH_REPLACE_OWF = 11;
        internal const uint ALG_SID_SHA_256 = 12;

        // cert encoding flags.
        internal const uint CRYPT_ASN_ENCODING = 0x00000001;
        internal const uint CRYPT_NDR_ENCODING = 0x00000002;
        internal const uint X509_ASN_ENCODING = 0x00000001;
        internal const uint X509_NDR_ENCODING = 0x00000002;
        internal const uint PKCS_7_ASN_ENCODING = 0x00010000;
        internal const uint PKCS_7_NDR_ENCODING = 0x00020000;
        internal const uint PKCS_7_OR_X509_ASN_ENCODING = (PKCS_7_ASN_ENCODING | X509_ASN_ENCODING);

        internal const uint CALG_SHA1 = (ALG_CLASS_HASH | ALG_TYPE_ANY | ALG_SID_SHA1);
        internal const uint CALG_SHA_256 = (ALG_CLASS_HASH | ALG_TYPE_ANY | ALG_SID_SHA_256);

        // CERT_STRONG_SIGN_PARA policy constants
        // https://msdn.microsoft.com/en-us/library/windows/desktop/hh870262(v=vs.85).aspx
        internal const string szOID_CERT_STRONG_SIGN_OS_1 = "1.3.6.1.4.1.311.72.1.1";
        internal const string szOID_CERT_STRONG_KEY_OS_1 = "1.3.6.1.4.1.311.72.2.1";

        [DllImport("winterop.dll", EntryPoint = "HashPublicKeyInfo", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
        private static extern void HashPublicKeyInfo(IntPtr certContext, byte[] publicKeyInfoHashed, ref uint sizePublicKeyInfoHashed);

        internal static void HashPublicKeyInfoWrapper(IntPtr certContext, byte[] publicKeyInfoHashed, ref uint sizePublicKeyInfoHashed)
        {
            BinaryParsers.PlatformSpecificHelpers.ThrowIfNotOnWindows();
            HashPublicKeyInfo(certContext, publicKeyInfoHashed, ref sizePublicKeyInfoHashed);
        }

        [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern
        bool CryptHashPublicKeyInfo(
            [In] IntPtr hCryptProv, // HCRYPTPROV
            [In] uint Algid,      // ALG_ID
            [In] uint dwFlags,
            [In] uint dwCertEncodingType,
            [In] ref CERT_PUBLIC_KEY_INFO pInfo,
            [Out] byte[] pbComputedHash,
            [In, Out] ref uint pcbComputedHash);

        internal static bool CryptHashPublicKeyInfoWrapper(
            [In] IntPtr hCryptProv, // HCRYPTPROV
            [In] uint Algid,      // ALG_ID
            [In] uint dwFlags,
            [In] uint dwCertEncodingType,
            [In] ref CERT_PUBLIC_KEY_INFO pInfo,
            [Out] byte[] pbComputedHash,
            [In, Out] ref uint pcbComputedHash)
        {
            BinaryParsers.PlatformSpecificHelpers.ThrowIfNotOnWindows();
            return CryptHashPublicKeyInfo(hCryptProv, Algid, dwFlags, dwCertEncodingType, ref pInfo, pbComputedHash, ref pcbComputedHash);
        }

        [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern
        bool CryptHashToBeSigned(
            [In] IntPtr hCryptProv, // HCRYPTPROV
            [In] uint dwCertEncodingType,
            [In] byte[] pbEncoded,
            [In] uint cbEncoded,
            [Out] byte[] pbComputedHash,
            [In, Out] ref uint pcbComputedHash);

        internal static bool CryptHashToBeSignedWrapper(
            [In] IntPtr hCryptProv, // HCRYPTPROV
            [In] uint dwCertEncodingType,
            [In] byte[] pbEncoded,
            [In] uint cbEncoded,
            [Out] byte[] pbComputedHash,
            [In, Out] ref uint pcbComputedHash)
        {
            BinaryParsers.PlatformSpecificHelpers.ThrowIfNotOnWindows();
            return CryptHashToBeSignedWrapper(hCryptProv,
                dwCertEncodingType,
                pbEncoded,
                cbEncoded,
                pbComputedHash,
                ref pcbComputedHash);
        }

        [DllImport("wintrust.dll")]
        private static extern uint WinVerifyTrust(
            IntPtr hwnd,
            ref Guid action,
            [In, Out] ref WINTRUST_DATA winVerifyTrustData);

        public static uint WinVerifyTrustWrapper(
            IntPtr hwnd,
            ref Guid action,
            [In, Out] ref WINTRUST_DATA winVerifyTrustData)
        {
            BinaryParsers.PlatformSpecificHelpers.ThrowIfNotOnWindows();
            return WinVerifyTrust(hwnd, ref action, ref winVerifyTrustData);
        }

        [DllImport("wintrust.dll")]
        private static extern IntPtr WTHelperProvDataFromStateData(
            IntPtr hStateData);

        public static IntPtr WTHelperProvDataFromStateDataWrapper(IntPtr hStateData)
        {
            BinaryParsers.PlatformSpecificHelpers.ThrowIfNotOnWindows();
            return WTHelperProvDataFromStateData(hStateData);
        }

        [DllImport("wintrust.dll")]
        private static extern IntPtr WTHelperGetProvSignerFromChain(
            IntPtr pProvData,
            uint idxSigner,
            [MarshalAsAttribute(UnmanagedType.Bool)] bool fCounterSigner,
            uint idxCounterSigner);

        public static IntPtr WTHelperGetProvSignerFromChainWrapper(
            IntPtr pProvData,
            uint idxSigner,
            [MarshalAsAttribute(UnmanagedType.Bool)] bool fCounterSigner,
            uint idxCounterSigner)
        {
            BinaryParsers.PlatformSpecificHelpers.ThrowIfNotOnWindows();
            return WTHelperGetProvSignerFromChain(pProvData, idxSigner, fCounterSigner, idxCounterSigner);
        }

        public enum UIChoice : uint
        {
            WTD_UI_ALL = 1,
            WTD_UI_NONE = 2,
            WTD_UI_NOBAD = 3,
            WTD_UI_NOGOOD = 4,
        }

        public enum RevocationChecks : uint
        {
            WTD_REVOKE_NONE = 0,
            WTD_REVOKE_WHOLECHAIN = 1,
        }

        public enum UnionChoice : uint
        {
            WTD_CHOICE_FILE = 1,
            WTD_CHOICE_CATALOG = 2,
            WTD_CHOICE_BLOB = 3,
            WTD_CHOICE_SIGNER = 4,
            WTD_CHOICE_CERT = 5,
        }

        public enum StateAction : uint
        {
            WTD_STATEACTION_IGNORE = 0,
            WTD_STATEACTION_VERIFY = 1,
            WTD_STATEACTION_CLOSE = 2,
            WTD_STATEACTION_AUTO_CACHE = 3,
            WTD_STATEACTION_AUTO_CACHE_FLUSH = 4,
        }

        [Flags]
        public enum ProviderFlags : uint
        {
            WTD_USE_IE4_TRUST_FLAG = 0x00000001,
            WTD_NO_IE4_CHAIN_FLAG = 0x00000002,
            WTD_NO_POLICY_USAGE_FLAG = 0x00000004,
            WTD_REVOCATION_CHECK_NONE = 0x00000010,
            WTD_REVOCATION_CHECK_END_CERT = 0x00000020,
            WTD_REVOCATION_CHECK_CHAIN = 0x00000040,
            WTD_REVOCATION_CHECK_CHAIN_EXCLUDE_ROOT = 0x00000080,
            WTD_SAFER_FLAG = 0x00000100,
            WTD_HASH_ONLY_FLAG = 0x00000200,
            WTD_USE_DEFAULT_OSVER_CHECK = 0x00000400,
            WTD_LIFETIME_SIGNING_FLAG = 0x00000800,
            WTD_CACHE_ONLY_URL_RETRIEVAL = 0x00001000, // affects CRL retrieval and AIA retrieval
            WTD_PROV_FLAGS_MASK = 0x0000ffff,
        }

        public enum UIContext : uint
        {
            WTD_UICONTEXT_EXECUTE = 0,
            WTD_UICONTEXT_INSTALL = 1,
        }

        public enum SignatureSettingsFlags : uint
        {
            WSS_VERIFY_SPECIFIC = 1,         // Indicates dwIndex is specified in WINTRUST_SIGNATURE_SETTINGS
            WSS_GET_SECONDARY_SIG_COUNT = 2, // Return the # of secondary signatures in cSecondarySigns
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CertChainPolicyPara
        {
            public uint cbSize;
            public uint dwFlags;
            public IntPtr pvExtraPolicyPara;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CertChainPolicyStatus
        {
            public uint cbSize;
            public uint dwError;
            public IntPtr lChainIndex;
            public IntPtr lElementIndex;
            public IntPtr pvExtraPolicyStatus;
        }

        public enum InfoChoice : uint
        {
            CERT_STRONG_SIGN_SERIALIZED_INFO_CHOICE = 1,
            CERT_STRONG_SIGN_OID_INFO_CHOICE
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CERT_STRONG_SIGN_PARA
        {
            public uint cbStruct;
            public InfoChoice dwInfoChoice;
            //    union
            //    {
            //        void* pvInfo;
            //        PCERT_STRONG_SIGN_SERIALIZED_INFO pSerializedInfo;
            //        LPSTR pszOID;
            //    };
            [MarshalAs(UnmanagedType.LPStr)]
            public string pszOID;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINTRUST_SIGNATURE_SETTINGS
        {
            public uint cbStruct;
            public uint dwIndex;
            public SignatureSettingsFlags dwFlags;
            public uint cSecondarySigs;
            public uint dwVerifiedSigIndex;
            public IntPtr pCryptoPolicy;  // *CERT_STRONG_SIGN_PARA
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINTRUST_DATA
        {
            public uint cbStruct;

            public IntPtr pPolicyCallbackData;   // optional: used to pass data between the app and policy
            public IntPtr pSIPClientData;        // optional: used to pass data between the app and SIP.
            public UIChoice UIChoice;
            public RevocationChecks RevocationChecks;
            //    union
            //    {
            //        struct WINTRUST_FILE_INFO_      *pFile;         // individual file
            //        struct WINTRUST_CATALOG_INFO_   *pCatalog;      // member of a Catalog File
            //        struct WINTRUST_BLOB_INFO_      *pBlob;         // memory blob
            //        struct WINTRUST_SGNR_INFO_      *pSgnr;         // signer structure only
            //        struct WINTRUST_CERT_INFO_      *pCert;
            //    };
            public UnionChoice UnionChoice;

            public IntPtr pFile;

            public StateAction StateAction;
            public IntPtr hWVTStateData;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pwszURLReference;  // optional: (future) used to determine zone.

            public ProviderFlags dwProvFlags;
            public UIContext UIContext;

            public IntPtr pSignatureSettings;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINTRUST_FILE_INFO
        {
            public uint cbStruct;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pcwszFilePath;
            public IntPtr hFile;
            public IntPtr pgKnownSubject;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct CRYPT_PROVIDER_SGNR
        {
            public uint cbStruct;
            public _FILETIME sftVerifyAsOf;
            public uint csCertChain;

            /// <summary>
            /// _CRYPT_PROVIDER_CERT*
            /// </summary>
            public IntPtr pasCertChain;

            public uint dwSignerType;

            /// <summary>
            /// CMSG_SIGNER_INFO*
            /// </summary>
            public IntPtr psSigner;

            public uint dwError;
            public uint csCounterSigners;

            /// <summary>
            /// _CRYPT_PROVIDER_SGNR*
            /// </summary>
            public IntPtr pasCounterSigners;

            /// <summary>
            /// PCCERT_CHAIN_CONTEXT->CERT_CHAIN_CONTEXT*
            /// </summary>
            public IntPtr pChainContext;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct CMSG_SIGNER_INFO
        {
            public uint dwVersion;
            public CERT_NAME_BLOB Issuer;
            public CRYPT_INTEGER_BLOB SerialNumber;
            public CRYPT_ALGORITHM_IDENTIFIER HashAlgorithm;
            public CRYPT_ALGORITHM_IDENTIFIER HashEncryptionAlgorithm;
            public CRYPT_DATA_BLOB EncryptedHash;
            public CRYPT_ATTRIBUTES AuthAttrs;
            public CRYPT_ATTRIBUTES UnauthAttrs;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CERT_NAME_BLOB
        {
            public int cbData;
            public IntPtr pbData;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CRYPT_INTEGER_BLOB
        {
            public int cbData;
            public IntPtr pbData;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CRYPT_DATA_BLOB
        {
            public int cbData;
            public IntPtr pbData;
        }
    }
}
