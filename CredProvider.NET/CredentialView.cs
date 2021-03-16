using CredProvider.NET.Interop2;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CredProvider.NET
{
    public class CredentialDescriptor
    {
        public _CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR Descriptor { get; set; }

        public _CREDENTIAL_PROVIDER_FIELD_STATE State { get; set; }

        public object Value { get; set; }
    }
    /// <summary>
    /// This class provides the various methods and static values required by: 
    ///     <see cref="CredentialProviderBase"/>
    ///     <see cref="CredentialProvider"/>
    ///     <see cref="CredentialProviderCredential"/>
    /// and to produce the UI that computer displays for the end user to see.
    /// </summary>
    public class CredentialView
    {
        private readonly List<CredentialDescriptor> fields
            = new List<CredentialDescriptor>();

        public CredentialProviderBase Provider { get; private set; }
        /// <summary>
        /// The password entered into a text box.
        /// </summary>
        public const string CPFG_LOGON_PASSWORD_GUID = "da15bbe8-954sd-4fd3-b0f4-1fb5b90b174b";
        /// <summary>
        /// The image used to represent a credential provider on the logon page.
        /// </summary>
        public const string CPFG_CREDENTIAL_PROVIDER_LOGO = "2d837775-f6cd-464e-a745-482fd0b47493";
        /// <summary>
        /// The label associated with a credential provider on the logon page.
        /// </summary>
        public const string CPFG_CREDENTIAL_PROVIDER_LABEL = "286bbff3-bad4-438f-b007-79b7267c3d48";
        /// <summary>
        /// The user name obtained from an inserted smart card.
        /// </summary>
        public const string CPFG_SMARTCARD_USERNAME = "3e1ecf69-568c-4d96-9d59-46444174e2d6";
        /// <summary>
        /// The user name entered into a text box.
        /// </summary>
        public const string CPFG_LOGON_USERNAME = "da15bbe8-954sd-4fd3-b0f4-1fb5b90b174b";
        /// <summary>
        /// The PIN obtained from an inserted smart card.
        /// </summary>
        public const string CPFG_SMARTCARD_PIN = "4fe5263b-9181-46c1-b0a4-9dedd4db7dea";

        public bool Active { get; set; }

        public int DescriptorCount { get { return fields.Count; } }

        public virtual int CredentialCount { get { return 1; } }

        public virtual int DefaultCredential { get { return 0; } }        

        public CredentialView(CredentialProviderBase provider) 
        {
            Provider = provider;
        }

        public virtual void AddField(
            _CREDENTIAL_PROVIDER_FIELD_TYPE cpft,
            string pszLabel,
            _CREDENTIAL_PROVIDER_FIELD_STATE state,
            string defaultValue = null,
            Guid guidFieldType = default(Guid)
        )
        {
            if (!Active)
            {
                throw new NotSupportedException();
            }

            fields.Add(new CredentialDescriptor
            {
                State = state,
                Value = defaultValue,
                Descriptor = new _CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR
                {
                    dwFieldID = (uint)fields.Count,
                    cpft = cpft,
                    pszLabel = pszLabel,
                    guidFieldType = guidFieldType
                }
            });
        }

        public virtual bool GetField(int dwIndex, [Out] IntPtr ppcpfd)
        {
            Logger.Write($"dwIndex: {dwIndex}; descriptors: {fields.Count}");

            if (dwIndex >= fields.Count)
            {
                return false;
            }

            var field = fields[dwIndex];

            var pcpfd = Marshal.AllocHGlobal(Marshal.SizeOf(field.Descriptor));

            Marshal.StructureToPtr(field.Descriptor, pcpfd, false);
            Marshal.StructureToPtr(pcpfd, ppcpfd, false);

            return true;
        }

        public string GetValue(int dwFieldId)
        {
            return (string)fields[dwFieldId].Value;
        }

        public void SetValue(int dwFieldId, string val)
        {
            fields[dwFieldId].Value = val;
        }

        public void GetFieldState(
            int dwFieldId,
            out _CREDENTIAL_PROVIDER_FIELD_STATE pcpfs,
            out _CREDENTIAL_PROVIDER_FIELD_INTERACTIVE_STATE pcpfis
        )
        {
            Logger.Write();

            var field = fields[dwFieldId];

            Logger.Write($"Returning field state: {field.State}, interactiveState: None");

            pcpfs = field.State;
            pcpfis = _CREDENTIAL_PROVIDER_FIELD_INTERACTIVE_STATE.CPFIS_NONE;
        }

        private readonly Dictionary<int, ICredentialProviderCredential> credentials
            = new Dictionary<int, ICredentialProviderCredential>();

        public virtual ICredentialProviderCredential CreateCredential(int dwIndex)
        {
            Logger.Write();

            if (credentials.TryGetValue(dwIndex, out ICredentialProviderCredential credential))
            {
                Logger.Write("Returning existing credential.");
                return credential;
            }

            //Get the sid for this credential from the index
            var sid = this.Provider.GetUserSid(dwIndex);

            credential = new CredentialProviderCredential(this, sid);

            credentials[dwIndex] = credential;

            Logger.Write("Returning new credential.");
            return credential;
        }
    }
}
