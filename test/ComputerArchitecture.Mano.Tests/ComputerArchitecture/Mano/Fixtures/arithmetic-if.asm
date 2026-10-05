/ Practice Q5 / review notes 7.4: subtraction and three-way arithmetic IF.
/ A-B must fit the signed 16-bit range for a sign-based comparison.
        ORG 100
        LDA B
        CMA
        INC
        ADD A
        SNA
        BUN CHK
        BUN L20
CHK,    SZA
        BUN L30
        BUN L25
L20,    LDA NEG
        BUN SAV
L25,    CLA
        BUN SAV
L30,    LDA POS
SAV,    STA RES
        HLT
A,      DEC 0
B,      DEC 0
NEG,    DEC -1
POS,    DEC 1
RES,    HEX 0
        END
