/ Review notes 8.2: parameter word immediately follows BSA.
        ORG 100
        BSA SUB
        HEX 3AF6
        STA RES
        HLT
SUB,    HEX 0
        LDA SUB I
        ISZ SUB
        BUN SUB I
RES,    HEX 0
        END
