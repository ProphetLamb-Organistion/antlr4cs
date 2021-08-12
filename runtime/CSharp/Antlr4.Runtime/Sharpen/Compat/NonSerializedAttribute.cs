﻿#if !PORTABLE

using System;
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(NonSerializedAttribute))]

#else

// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class: NonSerializedAttribute
**
**
** Purpose: Used to mark a member as being not-serialized
**
**
============================================================*/
namespace System
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    //[System.Runtime.InteropServices.ComVisible(true)]
    internal sealed class NonSerializedAttribute : Attribute
    {
    }
}

#endif
