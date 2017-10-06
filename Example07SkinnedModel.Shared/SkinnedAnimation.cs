using Impression;
using Impression.Graphics;
using Impression.Animations;
using System;
using System.Collections.Generic;

namespace Example07SkinnedModel
{
	public sealed class SkinnedAnimation
	{
		#region Nested
		public sealed class Bone
		{
			public string Name { get; set; }
			public Transform Transform { get; }

			public Bone()
			{
				Transform = new Transform(this);
			}
		}

		public sealed class Transform
		{
			Transform parent;

			public Bone Bone { get; private set; }

			public Transform Parent 
			{ 
				get { return parent;  }
				set
				{
					if (parent == value)
						return;

					var oldParent = parent;
					if (oldParent != null)
						oldParent.Children.Remove(this);

					parent = value;
					if (parent != null)
						parent.Children.Add(this);
				}
			}

			public List<Transform> Children { get; }

			public Vector3 LocalPosition
			{
				get; set;
			}

			public Quaternion LocalRotation
			{
				get; set;
			}

			public Vector3 LocalScale
			{
				get; set;
			}

			public Matrix Local
			{
				get
				{
					return Matrix.CreateScale(LocalScale) * Matrix.CreateFromQuaternion(LocalRotation) * Matrix.CreateTranslation(LocalPosition);
				}
				set
				{
					Vector3 scale;
					Quaternion rotation;
					Vector3 translation;

					if (value.Decompose(out scale, out rotation, out translation))
					{
						this.LocalScale = scale;
						this.LocalRotation = rotation;
						this.LocalPosition = translation;
					}
				}
			}

			public Matrix World
			{
				get 
				{
					return Parent != null ? Local * Parent.World : Local; 
				}
			}

			public Transform(Bone bone)
			{
				this.Bone = bone;
				this.Children = new List<Transform>();

				this.LocalPosition = Vector3.Zero;
				this.LocalRotation = Quaternion.Identity;
				this.LocalScale = Vector3.One;
			}
		}
		#endregion

		List<Bone> flattenBones;
		List<int> linkedBones;

		Matrix[] bindPose;
		Matrix[] inverseBindPose;
		Matrix[] worldTransforms;
		Matrix[] skinTransforms;

		Animation animation;

		public Bone Skeleton 
		{ 
			get { return flattenBones[0]; } 
		}
		public Matrix[] SkinTransforms 
		{ 
			get { return skinTransforms; } 
		}
		public bool IsBindPosed
		{
			get; set;
		}

		public Animation Animation { get { return animation;  } }

		public SkinnedAnimation(Model model)
		{
			var linkedBoneMap = new Dictionary<ModelBone, Bone>();
			flattenBones = new List<Bone>();
			linkedBones = new List<int>();
			for (int i = 1; i < model.Bones.Count; i++)
			{
				var bone = new Bone() { Name = model.Bones[i].Name };
				bone.Transform.Local = model.Bones[i].Transform;

				if (i == 1)
				{
					linkedBones.Add(-1);
				}
				else
				{
					var parentBone = linkedBoneMap[model.Bones[i].Parent];
					bone.Transform.Parent = parentBone.Transform;

					linkedBones.Add(flattenBones.IndexOf(parentBone));
				}

				flattenBones.Add(bone);
				linkedBoneMap.Add(model.Bones[i], bone);
			}
			linkedBoneMap.Clear();

			bindPose = new Matrix[flattenBones.Count];
			inverseBindPose = new Matrix[flattenBones.Count];
			for (int i = 0; i < flattenBones.Count; i++)
			{
				var bone = flattenBones[i];

				if (i == 0)
					bindPose[i] = bone.Transform.Local;
				else
				{
					bindPose[i] = bone.Transform.Local * bindPose[linkedBones[i]];
				}

				inverseBindPose[i] = Matrix.Invert(bindPose[i]);
			}

			worldTransforms = new Matrix[flattenBones.Count];
			skinTransforms = new Matrix[flattenBones.Count];
			for (var i = 0; i < worldTransforms.Length; i++)
				worldTransforms[i] = Matrix.Identity;
			for (var i = 0; i < skinTransforms.Length; i++)
				skinTransforms[i] = Matrix.Identity;

			animation = new Animation(this, OnSamplerCollectionBuilder);
		}

		static List<ISampler> OnSamplerCollectionBuilder(object target, IDictionary<string, Curve> curves)
		{
			var map = new List<Tuple<Bone, string, ISampler>>();

			var root = (target as SkinnedAnimation).Skeleton;

			foreach (var pair in curves)
			{
				var pathSegment = pair.Key.Split('/');
				var objectSegment = pathSegment[pathSegment.Length - 1].Split(':');
				var propertySegment = objectSegment[objectSegment.Length - 1].Split('.');

				if (propertySegment.Length > 2)
					throw new IndexOutOfRangeException("propertySegment.Length > 2");
				var bone = root;

				if (pathSegment.Length > 2 )
					for (int i = 1; i < pathSegment.Length - 1; i++)
					{
						bone = bone.Transform.Children.Find((child) => child.Bone.Name.Equals(pathSegment[i]))?.Bone;

						if (bone == null)
							break;
					}
				else
				{
					if (!bone.Name.Equals(pathSegment[0]))
						throw new ArgumentOutOfRangeException("pathSegment");
				}

				if (bone == null)
				{
					continue;
				}

				var sampler = map.Find((t) => t.Item1 == bone && t.Item2.Equals(propertySegment[0]))?.Item3;

				if (sampler == null)
				{
					var propertyInfo = ReflectionHelper.GetProperty(bone.Transform, propertySegment[0]);

					if (propertyInfo != null)
					{
						switch (propertyInfo.PropertyType.Name)
						{
							case "Vector3":
								{
                                    var getter = ReflectionHelper.GetterForProperty<Func<Vector3>>(bone.Transform, propertySegment[0]);
                                    var setter = ReflectionHelper.SetterForProperty<Action<Vector3>>(bone.Transform, propertySegment[0]);

                                    sampler = new Vector3Sampler(getter, setter);
								}
								break;
							case "Quaternion":
								{
									var getter = ReflectionHelper.GetterForProperty<Func<Quaternion>>(bone.Transform, propertySegment[0]);
									var setter = ReflectionHelper.SetterForProperty<Action<Quaternion>>(bone.Transform, propertySegment[0]);

									sampler = new QuaternionSampler(getter, setter);
								}
								break;
						}

						if (sampler != null)
							map.Add(Tuple.Create(bone, propertySegment[0], sampler));
					}
				}

				if (sampler != null)
					sampler.SetCurve(pair.Key, pair.Value);
			}

			var samplers = new List<ISampler>();
			foreach (var item in map)
				samplers.Add(item.Item3);

			return samplers;
		}

		public void Update(GameTime gameTime)
		{
			if (IsBindPosed)
			{
				for (int i = 0; i < flattenBones.Count; i++)
				{
					worldTransforms[i] = bindPose[i];
				}
			}
			else
			{
				animation.Update((float)(gameTime.ElapsedGameTime.TotalSeconds));
				animation.Sample();

				for (int i = 0; i < flattenBones.Count; i++)
				{
					var parentIndex = linkedBones[i];
					var parentWorld = Matrix.Identity;

					if (parentIndex == -1)
						worldTransforms[i] = flattenBones[i].Transform.Local;
					else
						worldTransforms[i] = flattenBones[i].Transform.Local * worldTransforms[parentIndex];
				}
			}

			for (int i = 0; i < flattenBones.Count; i++)
			{
				skinTransforms[i] = inverseBindPose[i] * worldTransforms[i];
			}
		}
	}
}