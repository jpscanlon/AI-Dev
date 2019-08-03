CREATE TABLE [dbo].[ud_word] (
    [id]                         INT          IDENTITY (1, 1) NOT NULL,
    [type]                       VARCHAR (50) NOT NULL,
    [base]                       VARCHAR (50) NOT NULL,
    [base_spoken_rec]            VARCHAR (50) NULL,
    [base_spoken_synth]          VARCHAR (50) NULL,
    [noun_plural]                VARCHAR (50) NULL,
    [noun_plural_spoken_rec]     VARCHAR (50) NULL,
    [noun_plural_spoken_synth]   VARCHAR (50) NULL,
    [verb_singular]              VARCHAR (50) NULL,
    [verb_singular_spoken_rec]   VARCHAR (50) NULL,
    [verb_singular_spoken_synth] VARCHAR (50) NULL,
    [verb_past]                  VARCHAR (50) NULL,
    [verb_past_spoken_rec]       VARCHAR (50) NULL,
    [verb_past_spoken_synth]     VARCHAR (50) NULL,
    CONSTRAINT [PK_ud_word] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [CK_type] CHECK ([type]='transverb' OR [type]='intransverb' OR [type]='classnoun' OR [type]='nondiscobjnoun' OR [type]='discobjnoun' OR [type]='adj'),
    CONSTRAINT [IX_ud_word] UNIQUE NONCLUSTERED ([base] ASC)
);

