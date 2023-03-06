# Unity_CustomUndo

## Description
A static class **CustomUndo** that allows you to create custom undo and redo actions linked to the default unity system.  
It includes a way to store some data that will be sent to the undo and redo behaviour you define.

## Limitations
 - Any assembly reload will make the created undo/redo entries do nothing.
 - On unity version 2022.1 or older, the system can break by spamming the undo and redo button. It will also add an ID to the name you provide.
