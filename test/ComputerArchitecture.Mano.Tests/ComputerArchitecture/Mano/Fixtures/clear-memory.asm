/ Review notes 7.6 / Practice Q7: clear 600-6FF inclusive.
        ORG 100
        LDA ADS
        STA PTR
        LDA NUM
        STA CTR
        CLA
LOP,    STA PTR I
        ISZ PTR
        ISZ CTR
        BUN LOP
        HLT
ADS,    HEX 600
PTR,    HEX 0
NUM,    DEC -256
CTR,    HEX 0
        END
