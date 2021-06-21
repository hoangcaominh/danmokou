﻿using System;
using Danmokou.Core;
using Danmokou.Services;
using Suzunoya.Entities;
using SuzunoyaUnity.Derived;
using SuzunoyaUnity.Mimics;
using UnityEngine;

namespace MiniProjects.VN {
public class Mayumi : SZYUCharacter {
    public override Color TextColor => new Color(.99f, .84f, .81f);
    public override Color UIColor => new Color(.90f, .69f, .22f);
    public override string Name => "Joutouguu Mayumi";
    
    public override void RollEvent() => DependencyInjection.SFXService.Request("x-bubble-2", SFXType.TypingSound);
}

public class MayumiMimic : PiecewiseCharacterMimic {
    public override Type[] CoreTypes => new[] {typeof(Mayumi)};
}

}