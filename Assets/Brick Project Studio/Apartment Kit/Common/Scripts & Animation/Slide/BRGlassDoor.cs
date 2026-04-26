using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SojaExiles

{
	public class BRGlassDoor : MonoBehaviour
	{

		public Animator openandclose;
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
					if (dist < 15)
					{
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
			print("you are opening");
			openandclose.Play("BRGlassDoorOpen");
			open = true;
			yield return new WaitForSeconds(.5f);
		}

		IEnumerator closing()
		{
			print("you are closing");
			openandclose.Play("BRGlassDoorClose");
			open = false;
			yield return new WaitForSeconds(.5f);
		}


	}
}