/ Review notes polling pattern, extended to echo three bytes.
        ORG 100
INW,    SKI
        BUN INW
        INP
OUW,    SKO
        BUN OUW
        OUT
        ISZ CTR
        BUN INW
        HLT
CTR,    DEC -3
        END
