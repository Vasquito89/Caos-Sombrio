using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SojaExiles

{

	public class Drawer_Pull_X : MonoBehaviour
	{

		public Animator pull_01;
		public bool open;
		private Transform player;

		void Start()
		{
			player = GameObject.FindGameObjectWithTag("Player").transform;
			open = false;
		}

		void OnMouseOver()
		{
			{
				if (player)
				{
					float dist = Vector3.Distance(player.position, transform.position);
					if (dist < 10)
					{
						print("object name");
						if (open == false)
						{
							if (Input.GetMouseButtonDown(0))
							{
								StartCoroutine(opening());
							}
						}
						else
						{
							if (open == true)
							{
								if (Input.GetMouseButtonDown(0))
								{
									StartCoroutine(closing());
								}
							}

						}

					}
				}

			}

		}

		IEnumerator opening()
		{
			print("you are opening the door");
			pull_01.Play("openpull_01");
			open = true;
			yield return new WaitForSeconds(.5f);
		}

		IEnumerator closing()
		{
			print("you are closing the door");
			pull_01.Play("closepush_01");
			open = false;
			yield return new WaitForSeconds(.5f);
		}


	}
}