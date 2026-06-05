"""Thin REPL: parse a line into command + argument and dispatch to TodoService.

Holds no business rules — it only parses input and prints output.
"""

from __future__ import annotations

from collections.abc import Callable

from .service import TodoService
from .store import JsonTodoStore


def main() -> None:
    service = TodoService(store=JsonTodoStore("todos.json"))

    print("Simple Todo. Type 'help' for commands, 'quit' to exit.")
    print_list(service)

    while True:
        try:
            line = input("> ")
        except EOFError:
            break  # EOF (e.g. piped input)

        text = line.strip()
        if not text:
            continue

        parts = text.split(maxsplit=1)
        command = parts[0].lower()
        arg = parts[1].strip() if len(parts) > 1 else ""

        if command in ("quit", "exit"):
            break

        if not handle_command(service, command, arg):
            print(f"Unknown command '{command}'. Type 'help'.")

    print("Bye.")


def handle_command(service: TodoService, command: str, arg: str) -> bool:
    if command == "help":
        print_help()
        return True

    if command == "list":
        print_list(service)
        return True

    if command == "add":
        try:
            item = service.add(arg)
            print(f"Added {item}")
        except ValueError as ex:
            print(f"Error: {ex}")
        return True

    if command == "done":
        apply_by_id(arg, service.complete, "Completed", "No item with id")
        return True

    if command == "reopen":
        apply_by_id(arg, service.reopen, "Reopened", "No item with id")
        return True

    if command in ("remove", "rm"):
        apply_by_id(arg, service.remove, "Removed", "No item with id")
        return True

    return False


def apply_by_id(
    arg: str, action: Callable[[int], bool], ok_verb: str, fail_msg: str
) -> None:
    try:
        id = int(arg)
    except ValueError:
        print(f"Expected a numeric id, got '{arg}'.")
        return

    print(f"{ok_verb} #{id}" if action(id) else f"{fail_msg} {id}.")


def print_list(service: TodoService) -> None:
    if service.count == 0:
        print("(no items)")
        return

    for item in service.get_all():
        print(f"  {item}")


def print_help() -> None:
    print(
        """Commands:
  add <title>     Add a new item
  list            Show all items
  done <id>       Mark item complete
  reopen <id>     Mark item not complete
  remove <id>     Delete an item (alias: rm)
  help            Show this help
  quit            Exit (alias: exit)"""
    )
