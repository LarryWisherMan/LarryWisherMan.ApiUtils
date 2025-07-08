
:::mermaid
classDiagram
    class ApiCmdletBase~TResult~ {
        +Uri BaseUri
        +Hashtable Headers
        +PSCredential Credential
        +SwitchParameter SkipCertificateCheck
        +SwitchParameter PassThru
        +ProcessRecord()
        <<abstract>>
        #InvokeCoreAsync(ct)
    }

    class SessionResolverExtensions {
        +GetSession(this PSCmdlet, string name)
        +SaveSession(...)
    }
    <<static>> SessionResolverExtensions

    ApiCmdletBase <|-- ConnectApiSession
    ApiCmdletBase <|-- DisconnectApiSession
    ApiCmdletBase <|-- InvokeApiRestMethod
    ApiCmdletBase <|-- InvokeApiRequest
    ApiCmdletBase <|-- NewApiSession
    ApiCmdletBase <|-- RemoveApiSession
    ApiCmdletBase <|-- SetApiSession
    ApiCmdletBase <|-- TestApiSession
:::
