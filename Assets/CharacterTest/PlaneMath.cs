using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneMath 
{
	public static Vector3 SetVectorLength(Vector3 vector, float size){
		//normalize the vector
		Vector3 vectorNormalized = Vector3.Normalize(vector);
		//scale the vector
		return vectorNormalized *= size;
	}
	
	public static Vector3 ProjectPointOnPlane(Vector3 planeNormal, Vector3 planePoint, Vector3 point){
		float distance;
		Vector3 translationVector;
		//First calculate the distance from the point to the plane:
		distance = SignedDistancePlanePoint(planeNormal, planePoint, point);
		//Reverse the sign of the distance
		distance *= -1;
		//Get a translation vector
		translationVector = SetVectorLength(planeNormal, distance);
		//Translate the point to form a projection
		return point + translationVector;
	}	
 
	//Get the shortest distance between a point and a plane. The output is signed so it holds information
	//as to which side of the plane normal the point is.
	public static float SignedDistancePlanePoint(Vector3 planeNormal, Vector3 planePoint, Vector3 point){
		return Vector3.Dot(planeNormal, (point - planePoint));
	}	
}
