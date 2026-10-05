/ Part 1 Notes pp.21-22 ISR save/restore pattern.
/ A service routine records one input byte and asks the main loop to stop.
        ORG 000
ZRO,    HEX 0
        BUN SRV
        ORG 100
        ION
        BUN LOP
LOP,    LDA FLG
        SZA
        BUN DON
        BUN LOP
DON,    HLT
FLG,    HEX 0
        ORG 200
SRV,    STA SAC
        CIR
        STA SE
        SKI
        BUN EXT
        INP
        STA CHR
        LDA ONE
        STA FLG
EXT,    LDA SE
        CIL
        LDA SAC
        ION
        BUN ZRO I
SAC,    HEX 0
SE,     HEX 0
CHR,    HEX 0
ONE,    DEC 1
        END
