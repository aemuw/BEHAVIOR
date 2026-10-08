using Godot;

//допоміжні перевірки "чи бачить гравець цю точку"
//потрібні, щоб гра змінювала світ тільки тоді, коли гравець не дивиться
public static class VisibilityUtil
{
	//точка видима, якщо вона в кадрі камери і між камерою та точкою немає перешкод
	//tolerance враховує, що промінь влучає в поверхню предмета, а не в його центр
	public static bool IsPointVisible(
		Camera3D camera,
		Vector3 point,
		float tolerance = 0.7f)
	{
		if (camera == null ||
			!GodotObject.IsInstanceValid(camera))
		{
			return false;
		}

		if (!camera.IsPositionInFrustum(point))
		{
			return false;
		}

		PhysicsRayQueryParameters3D query =
			PhysicsRayQueryParameters3D.Create(
				camera.GlobalPosition,
				point
			);

		Node player =
			camera.GetTree().GetFirstNodeInGroup("player");

		if (player is CollisionObject3D body)
		{
			query.Exclude =
				new Godot.Collections.Array<Rid>
				{
					body.GetRid()
				};
		}

		var hit =
			camera.GetWorld3D()
				.DirectSpaceState
				.IntersectRay(query);

		if (hit.Count == 0)
		{
			return true;
		}

		Vector3 hitPosition =
			hit["position"].AsVector3();

		return hitPosition.DistanceTo(point) <= tolerance;
	}
}
