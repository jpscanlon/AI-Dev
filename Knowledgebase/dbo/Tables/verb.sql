CREATE TABLE [dbo].[verb] (
    [word_id]               INT          NOT NULL,
    [word]                  VARCHAR (50) NOT NULL,
    [singular]              VARCHAR (50) NOT NULL,
    [past]                  VARCHAR (50) NOT NULL,
    [singular_spoken_rec]   VARCHAR (50) NULL,
    [singular_spoken_synth] VARCHAR (50) NULL,
    [past_spoken_rec]       VARCHAR (50) NULL,
    [past_spoken_synth]     VARCHAR (50) NULL,
    CONSTRAINT [PK_verb] PRIMARY KEY CLUSTERED ([word_id] ASC),
    CONSTRAINT [FK_ud_word_verb] FOREIGN KEY ([word_id]) REFERENCES [dbo].[ud_word] ([id]) ON DELETE CASCADE
);

