﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public struct CSVertexData
{
	public Vector3 position;
	public Vector3 normal;
}


public class CSParticleWorld : MonoBehaviour
{
	public const int MAX_SPHERE_COLLIDERS = 256;
	public const int MAX_CAPSULE_COLLIDERS = 256;
	public const int MAX_BOX_COLLIDERS = 256;

	public GameObject cam;
	public Material matCSParticle;
	public ComputeShader csParticle;

	public int kernelUpdateVelocity;
	public int kernelIntegrate;
	public ComputeBuffer cbSphereColliders;
	public ComputeBuffer cbCapsuleColliders;
	public ComputeBuffer cbBoxColliders;
	public ComputeBuffer cbCubeVertices;
	public List<CSParticleCollider> prevColliders = new List<CSParticleCollider>();


	void Start()
	{
		DSRenderer dscam = cam.GetComponent<DSRenderer>();
		dscam.AddCallbackPreGBuffer(() => { CSParticleSet.CSDepthPrePassAll(this); }, 999);
		dscam.AddCallbackPostGBuffer(() => { CSParticleSet.CSRenderAll(this); }, 1000);

		kernelUpdateVelocity = csParticle.FindKernel("UpdateVelocity");
		kernelIntegrate = csParticle.FindKernel("Integrate");

		cbCubeVertices = new ComputeBuffer(36, 24);
		{
			const float s = 0.05f;
			const float p = 1.0f;
			const float n = -1.0f;
			const float z = 0.0f;
			Vector3[] positions = new Vector3[24] {
				new Vector3(-s,-s, s), new Vector3( s,-s, s), new Vector3( s, s, s), new Vector3(-s, s, s),
				new Vector3(-s, s,-s), new Vector3( s, s,-s), new Vector3( s, s, s), new Vector3(-s, s, s),
				new Vector3(-s,-s,-s), new Vector3( s,-s,-s), new Vector3( s,-s, s), new Vector3(-s,-s, s),
				new Vector3(-s,-s, s), new Vector3(-s,-s,-s), new Vector3(-s, s,-s), new Vector3(-s, s, s),
				new Vector3( s,-s, s), new Vector3( s,-s,-s), new Vector3( s, s,-s), new Vector3( s, s, s),
				new Vector3(-s,-s,-s), new Vector3( s,-s,-s), new Vector3( s, s,-s), new Vector3(-s, s,-s),
			};
			Vector3[] normals = new Vector3[24] {
				new Vector3(z, z, p), new Vector3(z, z, p), new Vector3(z, z, p), new Vector3(z, z, p),
				new Vector3(z, p, z), new Vector3(z, p, z), new Vector3(z, p, z), new Vector3(z, p, z),
				new Vector3(z, n, z), new Vector3(z, n, z), new Vector3(z, n, z), new Vector3(z, n, z),
				new Vector3(n, z, z), new Vector3(n, z, z), new Vector3(n, z, z), new Vector3(n, z, z),
				new Vector3(p, z, z), new Vector3(p, z, z), new Vector3(p, z, z), new Vector3(p, z, z),
				new Vector3(z, z, n), new Vector3(z, z, n), new Vector3(z, z, n), new Vector3(z, z, n),
			};
			int[] indices = new int[36] {
				0,1,3, 3,1,2,
				5,4,6, 6,4,7,
				8,9,11, 11,9,10,
				13,12,14, 14,12,15,
				16,17,19, 19,17,18,
				21,20,22, 22,20,23,
			};
			CSVertexData[] vertices = new CSVertexData[36];
			for (int i = 0; i < vertices.Length; ++i)
			{
				vertices[i].position = positions[indices[i]];
				vertices[i].normal = normals[indices[i]];
			}
			cbCubeVertices.SetData(vertices);
		}

		// doesn't work on WebPlayer
		//Debug.Log("Marshal.SizeOf(typeof(CSSphereCollider))" + Marshal.SizeOf(typeof(CSSphereCollider)));
		//Debug.Log("Marshal.SizeOf(typeof(CSCapsuleCollider))" + Marshal.SizeOf(typeof(CSCapsuleCollider)));
		//Debug.Log("Marshal.SizeOf(typeof(CSBoxCollider))" + Marshal.SizeOf(typeof(CSBoxCollider)));
		cbSphereColliders = new ComputeBuffer(MAX_SPHERE_COLLIDERS, 44);
		cbCapsuleColliders = new ComputeBuffer(MAX_CAPSULE_COLLIDERS, 56);
		cbBoxColliders = new ComputeBuffer(MAX_BOX_COLLIDERS, 136);

		csParticle.SetBuffer(kernelUpdateVelocity, "sphere_colliders", cbSphereColliders);
		csParticle.SetBuffer(kernelUpdateVelocity, "capsule_colliders", cbCapsuleColliders);
		csParticle.SetBuffer(kernelUpdateVelocity, "box_colliders", cbBoxColliders);
	}

	protected void OnDisable()
	{
		cbSphereColliders.Release();
		cbCapsuleColliders.Release();
		cbBoxColliders.Release();
		cbCubeVertices.Release();
	}

	void Update()
	{
		CSParticleSet.HandleParticleCollisionAll(this);

		CSParticleCollider.UpdateCSColliders();
		cbSphereColliders.SetData(CSParticleCollider.csSphereColliders.ToArray());
		cbCapsuleColliders.SetData(CSParticleCollider.csCapsuleColliders.ToArray());
		cbBoxColliders.SetData(CSParticleCollider.csBoxColliders.ToArray());

		prevColliders.Clear();
		prevColliders.AddRange(CSParticleCollider.instances);
		CSParticleSet.UpdateParticleSetAll(this);
	}
}