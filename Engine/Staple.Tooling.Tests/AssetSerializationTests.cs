using Newtonsoft.Json;
using Staple.Internal;
using Staple.Tooling;

namespace StapleToolingTests;

public class AssetSerializationTests
{
    internal static readonly string AssetData = $$"""
{
  "guid": "dd8767a6-62bc-440b-9d7a-d087211be124",
  "typeName": "Staple.SkinnedAnimationStateMachine",
  "parameters": {
    "mesh": {
      "typeName": "Staple.Mesh",
      "value": "a0b56adb-c2b1-4f89-9ba2-06586936916f:0"
    },
    "states": {
      "typeName": "System.Collections.Generic.List`1[[Staple.SkinnedAnimationStateMachine+AnimationState, StapleCore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]",
      "value": [
        {
          "typeName": "Staple.SkinnedAnimationStateMachine+AnimationState",
          "parameters": {
            "name": {
              "typeName": "System.String",
              "value": "Idle"
            },
            "animation": {
              "typeName": "System.String",
              "value": "Idle"
            },
            "repeat": {
              "typeName": "System.Boolean",
              "value": true
            },
            "connections": {
              "typeName": "System.Collections.Generic.List`1[[Staple.SkinnedAnimationStateMachine+AnimationStateConnection, StapleCore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]",
              "value": [
                {
                  "typeName": "Staple.SkinnedAnimationStateMachine+AnimationStateConnection",
                  "parameters": {
                    "name": {
                      "typeName": "System.String",
                      "value": "Walk"
                    },
                    "any": {
                      "typeName": "System.Boolean",
                      "value": false
                    },
                    "onFinish": {
                      "typeName": "System.Boolean",
                      "value": false
                    },
                    "parameters": {
                      "typeName": "System.Collections.Generic.List`1[[Staple.SkinnedAnimationStateMachine+AnimationConditionParameter, StapleCore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]",
                      "value": [
                        {
                          "typeName": "Staple.SkinnedAnimationStateMachine+AnimationConditionParameter",
                          "parameters": {
                            "name": {
                              "typeName": "System.String",
                              "value": "Movement"
                            },
                            "condition": {
                              "typeName": "Staple.SkinnedAnimationStateMachine+AnimationCondition",
                              "value": 0
                            },
                            "boolValue": {
                              "typeName": "System.Boolean",
                              "value": true
                            },
                            "intValue": {
                              "typeName": "System.Int32",
                              "value": 0
                            },
                            "floatValue": {
                              "typeName": "System.Single",
                              "value": 0.0
                            }
                          }
                        }
                      ]
                    }
                  }
                },
                {
                  "typeName": "Staple.SkinnedAnimationStateMachine+AnimationStateConnection",
                  "parameters": {
                    "name": {
                      "typeName": "System.String",
                      "value": "Jump Start"
                    },
                    "any": {
                      "typeName": "System.Boolean",
                      "value": false
                    },
                    "onFinish": {
                      "typeName": "System.Boolean",
                      "value": false
                    },
                    "parameters": {
                      "typeName": "System.Collections.Generic.List`1[[Staple.SkinnedAnimationStateMachine+AnimationConditionParameter, StapleCore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]",
                      "value": [
                        {
                          "typeName": "Staple.SkinnedAnimationStateMachine+AnimationConditionParameter",
                          "parameters": {
                            "name": {
                              "typeName": "System.String",
                              "value": "Jump"
                            },
                            "condition": {
                              "typeName": "Staple.SkinnedAnimationStateMachine+AnimationCondition",
                              "value": 0
                            },
                            "boolValue": {
                              "typeName": "System.Boolean",
                              "value": true
                            },
                            "intValue": {
                              "typeName": "System.Int32",
                              "value": 0
                            },
                            "floatValue": {
                              "typeName": "System.Single",
                              "value": 0.0
                            }
                          }
                        }
                      ]
                    }
                  }
                }
              ]
            }
          }
        },
        {
          "typeName": "Staple.SkinnedAnimationStateMachine+AnimationState",
          "parameters": {
            "name": {
              "typeName": "System.String",
              "value": "Walk"
            },
            "animation": {
              "typeName": "System.String",
              "value": "Running_A"
            },
            "repeat": {
              "typeName": "System.Boolean",
              "value": true
            },
            "connections": {
              "typeName": "System.Collections.Generic.List`1[[Staple.SkinnedAnimationStateMachine+AnimationStateConnection, StapleCore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]",
              "value": [
                {
                  "typeName": "Staple.SkinnedAnimationStateMachine+AnimationStateConnection",
                  "parameters": {
                    "name": {
                      "typeName": "System.String",
                      "value": "Idle"
                    },
                    "any": {
                      "typeName": "System.Boolean",
                      "value": false
                    },
                    "onFinish": {
                      "typeName": "System.Boolean",
                      "value": false
                    },
                    "parameters": {
                      "typeName": "System.Collections.Generic.List`1[[Staple.SkinnedAnimationStateMachine+AnimationConditionParameter, StapleCore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]",
                      "value": [
                        {
                          "typeName": "Staple.SkinnedAnimationStateMachine+AnimationConditionParameter",
                          "parameters": {
                            "name": {
                              "typeName": "System.String",
                              "value": "Movement"
                            },
                            "condition": {
                              "typeName": "Staple.SkinnedAnimationStateMachine+AnimationCondition",
                              "value": 0
                            },
                            "boolValue": {
                              "typeName": "System.Boolean",
                              "value": false
                            },
                            "intValue": {
                              "typeName": "System.Int32",
                              "value": 0
                            },
                            "floatValue": {
                              "typeName": "System.Single",
                              "value": 0.0
                            }
                          }
                        }
                      ]
                    }
                  }
                },
                {
                  "typeName": "Staple.SkinnedAnimationStateMachine+AnimationStateConnection",
                  "parameters": {
                    "name": {
                      "typeName": "System.String",
                      "value": "Jump Start"
                    },
                    "any": {
                      "typeName": "System.Boolean",
                      "value": false
                    },
                    "onFinish": {
                      "typeName": "System.Boolean",
                      "value": false
                    },
                    "parameters": {
                      "typeName": "System.Collections.Generic.List`1[[Staple.SkinnedAnimationStateMachine+AnimationConditionParameter, StapleCore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]",
                      "value": [
                        {
                          "typeName": "Staple.SkinnedAnimationStateMachine+AnimationConditionParameter",
                          "parameters": {
                            "name": {
                              "typeName": "System.String",
                              "value": "Jump"
                            },
                            "condition": {
                              "typeName": "Staple.SkinnedAnimationStateMachine+AnimationCondition",
                              "value": 0
                            },
                            "boolValue": {
                              "typeName": "System.Boolean",
                              "value": true
                            },
                            "intValue": {
                              "typeName": "System.Int32",
                              "value": 0
                            },
                            "floatValue": {
                              "typeName": "System.Single",
                              "value": 0.0
                            }
                          }
                        }
                      ]
                    }
                  }
                }
              ]
            }
          }
        },
        {
          "typeName": "Staple.SkinnedAnimationStateMachine+AnimationState",
          "parameters": {
            "name": {
              "typeName": "System.String",
              "value": "Jump Start"
            },
            "animation": {
              "typeName": "System.String",
              "value": "Jump_Start"
            },
            "repeat": {
              "typeName": "System.Boolean",
              "value": false
            },
            "connections": {
              "typeName": "System.Collections.Generic.List`1[[Staple.SkinnedAnimationStateMachine+AnimationStateConnection, StapleCore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]",
              "value": [
                {
                  "typeName": "Staple.SkinnedAnimationStateMachine+AnimationStateConnection",
                  "parameters": {
                    "name": {
                      "typeName": "System.String",
                      "value": "Jump Land"
                    },
                    "any": {
                      "typeName": "System.Boolean",
                      "value": false
                    },
                    "onFinish": {
                      "typeName": "System.Boolean",
                      "value": false
                    },
                    "parameters": {
                      "typeName": "System.Collections.Generic.List`1[[Staple.SkinnedAnimationStateMachine+AnimationConditionParameter, StapleCore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]",
                      "value": [
                        {
                          "typeName": "Staple.SkinnedAnimationStateMachine+AnimationConditionParameter",
                          "parameters": {
                            "name": {
                              "typeName": "System.String",
                              "value": "Jump"
                            },
                            "condition": {
                              "typeName": "Staple.SkinnedAnimationStateMachine+AnimationCondition",
                              "value": 0
                            },
                            "boolValue": {
                              "typeName": "System.Boolean",
                              "value": false
                            },
                            "intValue": {
                              "typeName": "System.Int32",
                              "value": 0
                            },
                            "floatValue": {
                              "typeName": "System.Single",
                              "value": 0.0
                            }
                          }
                        }
                      ]
                    }
                  }
                }
              ]
            }
          }
        },
        {
          "typeName": "Staple.SkinnedAnimationStateMachine+AnimationState",
          "parameters": {
            "name": {
              "typeName": "System.String",
              "value": "Jump Land"
            },
            "animation": {
              "typeName": "System.String",
              "value": "Jump_Land"
            },
            "repeat": {
              "typeName": "System.Boolean",
              "value": false
            },
            "connections": {
              "typeName": "System.Collections.Generic.List`1[[Staple.SkinnedAnimationStateMachine+AnimationStateConnection, StapleCore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]",
              "value": [
                {
                  "typeName": "Staple.SkinnedAnimationStateMachine+AnimationStateConnection",
                  "parameters": {
                    "name": {
                      "typeName": "System.String",
                      "value": "Walk"
                    },
                    "any": {
                      "typeName": "System.Boolean",
                      "value": false
                    },
                    "onFinish": {
                      "typeName": "System.Boolean",
                      "value": true
                    },
                    "parameters": {
                      "typeName": "System.Collections.Generic.List`1[[Staple.SkinnedAnimationStateMachine+AnimationConditionParameter, StapleCore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]",
                      "value": []
                    }
                  }
                }
              ]
            }
          }
        }
      ]
    },
    "parameters": {
      "typeName": "System.Collections.Generic.List`1[[Staple.SkinnedAnimationStateMachine+AnimationParameter, StapleCore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]",
      "value": [
        {
          "typeName": "Staple.SkinnedAnimationStateMachine+AnimationParameter",
          "parameters": {
            "name": {
              "typeName": "System.String",
              "value": "Movement"
            },
            "parameterType": {
              "typeName": "Staple.SkinnedAnimationStateMachine+AnimationParameterType",
              "value": 0
            }
          }
        },
        {
          "typeName": "Staple.SkinnedAnimationStateMachine+AnimationParameter",
          "parameters": {
            "name": {
              "typeName": "System.String",
              "value": "Jump"
            },
            "parameterType": {
              "typeName": "Staple.SkinnedAnimationStateMachine+AnimationParameterType",
              "value": 0
            }
          }
        }
      ]
    }
  }
}
""";

    [Test]
    public void TestAssetExpansion()
    {
        var asset = JsonConvert.DeserializeObject<SerializableStapleAsset>(AssetData);

        Assert.That(asset, Is.Not.Null);

        Utilities.ExpandSerializedAsset(asset);

        Assert.Multiple(() =>
        {
            Assert.That(asset.typeName, Is.EqualTo("Staple.SkinnedAnimationStateMachine"));

            Assert.That(asset.parameters.Count, Is.EqualTo(3));

            Assert.That(asset.parameters.ContainsKey("mesh"));
            Assert.That(asset.parameters.ContainsKey("states"));
            Assert.That(asset.parameters.ContainsKey("parameters"));
        });

        var mesh = asset.parameters["mesh"];

        Assert.That(mesh.typeName, Is.EqualTo("Staple.Mesh"));
        Assert.That(mesh.value, Is.EqualTo("a0b56adb-c2b1-4f89-9ba2-06586936916f:0"));

        var states = asset.parameters["states"];

        Assert.That(states.value is List<object>);

        if(states.value is List<object> stateData)
        {
            Assert.That(stateData[0] is Dictionary<object, object>);
        }
    }
}